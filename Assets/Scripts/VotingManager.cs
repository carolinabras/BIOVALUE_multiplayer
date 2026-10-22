using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;

// Votes on the instruments placed on the board. Each placed piece opens its own ballot, so several
// instruments can be voted on at once (the GM can allow more than one per turn: MaxPerTurn).
// Players vote on each piece separately and can change their mind until the GM ends the turn, which
// closes every open ballot. A piece taken back to the tray (its ×) cancels its ballot for everyone.
// The vote is shown by VoteBubble (next to each piece), the player capsules (vote tokens), the turn
// ribbon (vote dots) and the result stamp on each piece.
public class VotingManager : MonoBehaviourPun
{
    public static VotingManager Instance;

    // One open vote: the piece, who placed it, the cell it came from (-1 = the tray) and the votes cast.
    public class Ballot
    {
        public int viewId;
        public int placer;
        public int oldCell;
        public readonly Dictionary<int, bool> votes = new Dictionary<int, bool>();

        public RectTransform Piece
        {
            get
            {
                var view = PhotonView.Find(viewId);
                return view ? view.transform as RectTransform : null;
            }
        }

        public string InstrumentName =>
            PhotonView.Find(viewId)?.GetComponent<InstrumentHook>()?.instrument?.name ?? "The instrument";

        public bool PlacedByGm => placer == GameAuthority.GmActor || (GameAuthority.GmActor < 0 && PhotonNetwork.MasterClient != null && placer == PhotonNetwork.MasterClient.ActorNumber);
        public int Yes => votes.Values.Count(v => v);
        public int No => votes.Values.Count(v => !v);
    }

    private readonly List<Ballot> _ballots = new List<Ballot>();
    public IReadOnlyList<Ballot> Ballots => _ballots;

    // Accumulated disagree (reject) vote count per actor across all voting rounds.
    private readonly Dictionary<int, int> _cumulativeDisagrees = new Dictionary<int, int>();

    public int GetDisagreeCount(int actorNumber) =>
        _cumulativeDisagrees.TryGetValue(actorNumber, out int c) ? c : 0;

    // Per placer (actor number), for the score formula: instruments put on the board, and how their votes ended.
    private readonly Dictionary<int, int> _placed   = new Dictionary<int, int>();
    private readonly Dictionary<int, int> _accepted = new Dictionary<int, int>();
    private readonly Dictionary<int, int> _rejected = new Dictionary<int, int>();

    // The latest vote on each instrument (by its number), as seen by this client.
    private static readonly Dictionary<int, (int yes, int no)> _resultsByInstrument = new Dictionary<int, (int yes, int no)>();
    public static bool TryGetResult(int instrumentId, out int yes, out int no)
    {
        bool found = _resultsByInstrument.TryGetValue(instrumentId, out var r);
        yes = r.yes; no = r.no;
        return found;
    }

    public int GetPlacedCount(int actorNumber)   => _placed.TryGetValue(actorNumber, out int c) ? c : 0;
    public int GetAcceptedCount(int actorNumber) => _accepted.TryGetValue(actorNumber, out int c) ? c : 0;
    public int GetRejectedCount(int actorNumber) => _rejected.TryGetValue(actorNumber, out int c) ? c : 0;

    private static void Count(Dictionary<int, int> counts, int actorNumber, int delta = 1)
    {
        if (actorNumber < 0) return;
        counts[actorNumber] = Mathf.Max(0, (counts.TryGetValue(actorNumber, out int c) ? c : 0) + delta);
    }

    // ── Instruments per turn ─────────────────────────────────────────────────

    public const string MaxPerTurnKey = "max_instruments_turn"; // int: instruments a player can put to the vote per turn, 0 = no limit
    public const int DefaultMaxPerTurn = 1;

    public static int MaxPerTurn =>
        PhotonNetwork.InRoom &&
        PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(MaxPerTurnKey, out var v) && v is int n
            ? n : DefaultMaxPerTurn;

    // GM only.
    public static void SetMaxPerTurn(int value)
    {
        if (!GameAuthority.IsGameMaster || !PhotonNetwork.InRoom) return;
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { MaxPerTurnKey, Mathf.Max(0, value) } });
    }

    // Pieces this actor has under vote now (this turn).
    public int PendingBy(int actorNumber) => _ballots.Count(b => b.placer == actorNumber);

    // Can the local player put another piece from the tray to the vote? The GM always can.
    public bool CanPlaceAnother()
    {
        if (GameAuthority.IsGameMaster) return true;
        int max = MaxPerTurn;
        return max <= 0 || PendingBy(PhotonNetwork.LocalPlayer.ActorNumber) < max;
    }

    // What the local player can still do on their turn, for the tray and the ribbon.
    public string PlaceHint()
    {
        int max = MaxPerTurn;
        int pending = PendingBy(PhotonNetwork.LocalPlayer.ActorNumber);
        if (max <= 0) return pending == 0 ? "Place as many instruments as you need" : "You can place more instruments";
        int left = max - pending;
        if (left <= 0)
            return max == 1 ? "Your instrument is being voted on" : "Your instruments are being voted on";
        if (max == 1) return "Place one instrument on the board";
        return pending == 0 ? $"Place up to {max} instruments" : left == 1 ? "You can place 1 more instrument" : $"You can place {left} more instruments";
    }

    // ── Events ────────────────────────────────────────────────────────────────
    public UnityEvent             OnVoteStarted = new UnityEvent();   // a ballot opened
    public UnityEvent<int, bool>  OnPlayerVoted = new UnityEvent<int, bool>(); // actor, their vote (on the ballot they changed)
    public UnityEvent             OnVoteEnded   = new UnityEvent();   // no ballot left open
    public UnityEvent             OnVotesChanged = new UnityEvent();  // any ballot opened, changed, cancelled or closed

    // The outcome of one ballot, on every client, as the votes close (the ribbon and the stamp on the piece show it).
    public class VoteResult
    {
        public bool approved;
        public int yes, no;
        public List<bool> votes = new List<bool>();
        public string instrumentName;
        public RectTransform piece;   // still on the cell it was voted on
    }
    public UnityEvent<VoteResult> OnVoteResult = new UnityEvent<VoteResult>();

    // A rejected piece stays on its cell this long before going back, so the result can play on it.
    [SerializeField] private float rejectedReturnDelay = 1.1f;

    private void Awake()
    {
        Instance = this;
        RestoreFromRoom();
    }

    // A rejoining client got the open votes in Awake: tell the vote bubbles, ribbon and capsules.
    private void Start()
    {
        if (_ballots.Count == 0) return;
        OnVoteStarted.Invoke();
        OnVotesChanged.Invoke();
    }

    // ── Kept in the room, for players who rejoin ─────────────────────────────

    // Open ballots, the per-player counts and the result of each instrument's vote, written by the
    // master client after every change (RejoinGuard).
    public const string StateKey = "vm_state";

    private void SaveToRoom()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
        string Counts(Dictionary<int, int> d) => string.Join(",", d.Select(kv => $"{kv.Key}:{kv.Value}"));
        var state = new Hashtable
        {
            { "b", string.Join(";", _ballots.Select(b =>
                $"{b.viewId},{b.placer},{b.oldCell}" + string.Concat(b.votes.Select(v => $",{v.Key}:{(v.Value ? 1 : 0)}")))) },
            { "p", Counts(_placed) },
            { "a", Counts(_accepted) },
            { "r", Counts(_rejected) },
            { "d", Counts(_cumulativeDisagrees) },
            { "i", string.Join(",", _resultsByInstrument.Select(kv => $"{kv.Key}:{kv.Value.yes}:{kv.Value.no}")) },
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { StateKey, state } });
    }

    private void RestoreFromRoom()
    {
        if (!PhotonNetwork.InRoom ||
            !PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(StateKey, out var raw) || !(raw is Hashtable state))
            return;
        string Get(string k) => state.TryGetValue(k, out var v) && v is string s ? s : "";
        void Fill(Dictionary<int, int> d, string text)
        {
            d.Clear();
            foreach (var pair in text.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = pair.Split(':');
                if (kv.Length == 2 && int.TryParse(kv[0], out int k) && int.TryParse(kv[1], out int n)) d[k] = n;
            }
        }

        _ballots.Clear();
        foreach (var text in Get("b").Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = text.Split(',');
            if (parts.Length < 3 || !int.TryParse(parts[0], out int id) || !int.TryParse(parts[1], out int placer) ||
                !int.TryParse(parts[2], out int oldCell))
                continue;
            var ballot = new Ballot { viewId = id, placer = placer, oldCell = oldCell };
            for (int i = 3; i < parts.Length; i++)
            {
                var kv = parts[i].Split(':');
                if (kv.Length == 2 && int.TryParse(kv[0], out int actor)) ballot.votes[actor] = kv[1] == "1";
            }
            _ballots.Add(ballot);
        }
        Fill(_placed, Get("p"));
        Fill(_accepted, Get("a"));
        Fill(_rejected, Get("r"));
        Fill(_cumulativeDisagrees, Get("d"));
        foreach (var text in Get("i").Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries))
        {
            var r = text.Split(':');
            if (r.Length == 3 && int.TryParse(r[0], out int inst) && int.TryParse(r[1], out int y) && int.TryParse(r[2], out int n))
                _resultsByInstrument[inst] = (y, n);
        }
    }

    // ── State ────────────────────────────────────────────────────────────────

    public bool IsVoting => _ballots.Count > 0;

    public Ballot Find(int viewId) => _ballots.FirstOrDefault(b => b.viewId == viewId);

    // The latest ballot (for the old single-vote readers).
    private Ballot Latest => _ballots.Count > 0 ? _ballots[_ballots.Count - 1] : null;
    public int PendingPlacer => Latest?.placer ?? -1;
    public RectTransform PendingPiece => Latest?.Piece;
    // Every open ballot was placed by the GM (a special action, not a player's turn).
    public bool PlacedByGm => _ballots.Count > 0 && _ballots.All(b => b.PlacedByGm);

    // Neither the GM nor whoever placed the piece votes on it.
    public bool CanVoteOn(Ballot b)
    {
        if (b == null) return false;
        // Debug/Editor only: the GM can vote too, which makes a solo preview possible. Never true in a release build.
        if (Debug.isDebugBuild && GameAuthority.IsGameMaster) return true;
        return !GameAuthority.IsGameMaster && b.placer != PhotonNetwork.LocalPlayer.ActorNumber;
    }

    public bool LocalCanVote => CanVoteOn(Latest);

    public static bool? VoteOf(Ballot b, int actor) =>
        b != null && b.votes.TryGetValue(actor, out bool v) ? v : (bool?)null;

    public bool? MyVoteOn(Ballot b) => VoteOf(b, PhotonNetwork.LocalPlayer.ActorNumber);
    public bool? MyVote => MyVoteOn(Latest);

    // How an actor voted across every open ballot: null until they voted on all of them;
    // true / false when all their votes agree; Mixed otherwise.
    public enum Summary { NotYet, Yes, No, Mixed }
    public Summary SummaryOf(int actor)
    {
        var mine = _ballots.Where(b => b.placer != actor).ToList();
        if (mine.Count == 0 || mine.Any(b => !b.votes.ContainsKey(actor))) return Summary.NotYet;
        bool allYes = mine.All(b => b.votes[actor]);
        bool allNo = mine.All(b => !b.votes[actor]);
        return allYes ? Summary.Yes : allNo ? Summary.No : Summary.Mixed;
    }

    // ── Start ─────────────────────────────────────────────────────────────────

    public void StartVote(int piecePhotonViewId, int newCellIndex, int oldCellIndex)
    {
        photonView.RPC(nameof(RPC_StartVote), RpcTarget.All, piecePhotonViewId, newCellIndex, oldCellIndex);
    }

    [PunRPC]
    private void RPC_StartVote(int piecePhotonViewId, int newCellIndex, int oldCellIndex)
    {
        var piece = PhotonView.Find(piecePhotonViewId);
        int placer = piece?.Owner?.ActorNumber ?? -1;
        string instrName = piece?.GetComponent<InstrumentHook>()?.instrument?.name ?? "instrument";

        // Moved while still under vote: the votes start again for the new spot; a rejection still sends
        // it back to where it was before this placement.
        var ballot = Find(piecePhotonViewId);
        if (ballot != null)
        {
            ballot.votes.Clear();
            ballot.placer = placer;
            OnVotesChanged.Invoke();
            SaveToRoom();
            return;
        }

        ballot = new Ballot { viewId = piecePhotonViewId, placer = placer, oldCell = oldCellIndex };
        _ballots.Add(ballot);
        Count(_placed, placer);

        OnVoteStarted.Invoke();
        OnVotesChanged.Invoke();
        SaveToRoom();

        if (ballot.PlacedByGm)
            GameLog.Instance?.AddEntryLocal(GameLog.Kind.Placed, $"{GameLog.B("GM")} made a special action: placed {GameLog.B(instrName)}");
        else
            GameLog.Instance?.AddEntryLocal(GameLog.Kind.Placed, $"{GameLog.B(GameLog.GetPlayerName(placer))} placed {GameLog.B(instrName)}");
    }

    // ── Votes ─────────────────────────────────────────────────────────────────

    public void Vote(int viewId, bool approved)
    {
        if (Find(viewId) == null) return;
        // The player can still change their mind until the GM ends the vote.
        photonView.RPC(nameof(RPC_SubmitVote), RpcTarget.All, viewId, approved);
    }

    // Old single-vote buttons: the latest ballot.
    public void OnClickApprove() { if (Latest != null) Vote(Latest.viewId, true); }
    public void OnClickReject()  { if (Latest != null) Vote(Latest.viewId, false); }

    [PunRPC]
    private void RPC_SubmitVote(int viewId, bool approved, PhotonMessageInfo info)
    {
        var ballot = Find(viewId);
        if (ballot == null) return;
        ballot.votes[info.Sender.ActorNumber] = approved; // replaces their previous vote
        OnPlayerVoted.Invoke(info.Sender.ActorNumber, approved);
        OnVotesChanged.Invoke();
        SaveToRoom();
    }

    // ── Cancel: the piece went back to the tray ──────────────────────────────

    // Called by the piece's owner. Returns the cell it came from (-1 = the tray), or false if it wasn't under vote.
    public bool TryCancelVote(int viewId, out int oldCell)
    {
        var ballot = Find(viewId);
        oldCell = ballot?.oldCell ?? -1;
        if (ballot == null) return false;
        photonView.RPC(nameof(RPC_CancelVote), RpcTarget.All, viewId);
        return true;
    }

    [PunRPC]
    private void RPC_CancelVote(int viewId)
    {
        var ballot = Find(viewId);
        if (ballot == null) return;
        _ballots.Remove(ballot);
        Count(_placed, ballot.placer, -1);
        string who = ballot.PlacedByGm ? "GM" : GameLog.GetPlayerName(ballot.placer);
        GameLog.Instance?.AddEntryLocal(GameLog.Kind.Placed,
            $"{GameLog.B(who)} took {GameLog.B(ballot.InstrumentName)} back, its vote was cancelled");
        SaveToRoom();
        OnVotesChanged.Invoke();
        if (_ballots.Count == 0) OnVoteEnded.Invoke();
    }

    // ── GM ends the votes + advances the turn ────────────────────────────────

    public void EndTurn()
    {
        if (!GameAuthority.IsGameMaster) return;

        // The GM's tallies are the reference for everyone.
        var ids = _ballots.Select(b => b.viewId).ToArray();
        var yes = _ballots.Select(b => b.Yes).ToArray();
        var no = _ballots.Select(b => b.No).ToArray();
        photonView.RPC(nameof(RPC_EndVotes), RpcTarget.All, ids, yes, no);

        // Advance to the next player (master broadcasts internally via GameState).
        GameState.Instance.GMAdvanceTurn();
    }

    [PunRPC]
    private void RPC_EndVotes(int[] ids, int[] yesCounts, int[] noCounts)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            var ballot = Find(ids[i]);
            if (ballot == null) continue;
            Close(ballot, yesCounts[i], noCounts[i]);
        }
        _ballots.Clear();
        SaveToRoom();
        OnVotesChanged.Invoke();
        OnVoteEnded.Invoke();
    }

    private void Close(Ballot ballot, int approveCount, int rejectCount)
    {
        bool approved = approveCount > rejectCount;
        Count(approved ? _accepted : _rejected, ballot.placer);

        var pieceView = PhotonView.Find(ballot.viewId);
        var instrument = pieceView != null ? pieceView.GetComponent<InstrumentHook>()?.instrument : null;
        if (instrument != null) _resultsByInstrument[instrument.id] = (approveCount, rejectCount);
        var drag = pieceView != null ? pieceView.GetComponent<DragPiece>() : null;
        if (drag) drag.HideCloseButton(); // decided: no more taking it back
        if (pieceView != null)
        {
            OnVoteResult.Invoke(new VoteResult
            {
                approved = approved,
                yes = approveCount,
                no = rejectCount,
                votes = new List<bool>(ballot.votes.Values),
                instrumentName = ballot.InstrumentName,
                piece = pieceView.transform as RectTransform
            });
        }

        // If rejected, send the instrument back to where it came from (after the result has played on it).
        if (!approved && drag)
            StartCoroutine(RevertAfterDelay(drag, ballot.oldCell));

        string votedName = GameLog.B(ballot.InstrumentName);
        string tally = $"{approveCount} yes · {rejectCount} no";
        if (approved)
            GameLog.Instance?.AddEntryLocal(GameLog.Kind.VotePassed, $"{votedName} approved", tally);
        else
            GameLog.Instance?.AddEntryLocal(GameLog.Kind.VoteFailed, $"{votedName} rejected, back to the tray", tally);

        foreach (var kvp in ballot.votes)
        {
            if (kvp.Value) continue; // approved, not a disagree
            _cumulativeDisagrees[kvp.Key] = GetDisagreeCount(kvp.Key) + 1;
        }
    }

    private IEnumerator RevertAfterDelay(DragPiece piece, int oldCellIndex)
    {
        if (rejectedReturnDelay > 0f) yield return new WaitForSecondsRealtime(rejectedReturnDelay);
        if (piece) piece.RevertToCell(oldCellIndex);
    }
}
