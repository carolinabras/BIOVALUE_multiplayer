using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;

public class VotingManager : MonoBehaviourPun
{
    public static VotingManager Instance;

    // The vote is shown by VoteBubble (next to the piece), the player capsules (vote tokens), the turn
    // ribbon (vote dots) and the result stamp on the piece.

    private int _votesApprove = 0;
    private int _votesReject  = 0;
    private int _totalVoters  = 0;

    // Tracks each voter's latest vote so changing mind replaces, not stacks.
    private readonly Dictionary<int, bool> _playerVotes = new Dictionary<int, bool>();

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

    private static void Count(Dictionary<int, int> counts, int actorNumber)
    {
        if (actorNumber < 0) return;
        counts[actorNumber] = (counts.TryGetValue(actorNumber, out int c) ? c : 0) + 1;
    }

    private int _pendingPiecePhotonViewId = -1;
    private int _pendingPlacerActorNumber = -1;
    private int _pendingOldCellIndex      = -1;

    // ── Events ────────────────────────────────────────────────────────────────
    public UnityEvent             OnVoteStarted = new UnityEvent();
    public UnityEvent<int, bool>  OnPlayerVoted = new UnityEvent<int, bool>();
    public UnityEvent             OnVoteEnded   = new UnityEvent();

    // The outcome, on every client, as the vote closes (the turn ribbon and the stamp on the piece show it).
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
    }

    // ── State for the vote bubble (VoteBubble) ───────────────────────────────

    public bool IsVoting => _pendingPiecePhotonViewId >= 0;
    public int PendingPlacer => _pendingPlacerActorNumber;
    // The piece being voted on was placed by the game master (a special action, not a player's turn).
    public bool PlacedByGm => PhotonNetwork.MasterClient != null && _pendingPlacerActorNumber == PhotonNetwork.MasterClient.ActorNumber;
    public bool LocalCanVote { get; private set; }

    // The piece being voted on, on its cell.
    public RectTransform PendingPiece
    {
        get
        {
            var view = _pendingPiecePhotonViewId >= 0 ? PhotonView.Find(_pendingPiecePhotonViewId) : null;
            return view ? view.transform as RectTransform : null;
        }
    }

    // This client's current vote: null before voting.
    public bool? MyVote =>
        _playerVotes.TryGetValue(PhotonNetwork.LocalPlayer.ActorNumber, out bool v) ? v : (bool?)null;

    // ── Start Vote ────────────────────────────────────────────────────────────

    public void StartVote(int piecePhotonViewId, int newCellIndex, int oldCellIndex)
    {
        photonView.RPC(nameof(RPC_StartVote), RpcTarget.All, piecePhotonViewId, newCellIndex, oldCellIndex);
    }

    [PunRPC]
    private void RPC_StartVote(int piecePhotonViewId, int newCellIndex, int oldCellIndex)
    {
        _votesApprove = 0;
        _votesReject  = 0;
        _playerVotes.Clear();
        _pendingPiecePhotonViewId = piecePhotonViewId;
        _pendingOldCellIndex      = oldCellIndex;

        _totalVoters = PhotonNetwork.PlayerList.Length - 2;

        bool isGM        = GameState.Instance.localPlayerIndex == 0;
        var  piece       = PhotonView.Find(piecePhotonViewId);
        bool isWhoPlaced = piece != null && piece.IsMine;

        _pendingPlacerActorNumber = piece?.Owner?.ActorNumber ?? -1;
        Count(_placed, _pendingPlacerActorNumber);

        OnVoteStarted.Invoke();

        // Debug/Editor only: the GM can vote too, since by design neither the GM nor whoever placed
        // the piece votes, which makes a solo preview impossible. Never true in a release build.
        bool debugForceShow = Debug.isDebugBuild && GameAuthority.IsGameMaster;

        LocalCanVote = (!isGM && !isWhoPlaced) || debugForceShow;

        string instrName  = piece?.GetComponent<InstrumentHook>()?.instrument?.name ?? "instrument";
        if (PlacedByGm)
            GameLog.Instance?.AddEntryLocal(GameLog.Kind.Placed, $"{GameLog.B("GM")} made a special action: placed {GameLog.B(instrName)}");
        else
            GameLog.Instance?.AddEntryLocal(GameLog.Kind.Placed, $"{GameLog.B(GameLog.GetPlayerName(_pendingPlacerActorNumber))} placed {GameLog.B(instrName)}");
    }

    // ── Player votes ──────────────────────────────────────────────────────────

    public void OnClickApprove()
    {
        // The player can still change their mind until the GM ends the vote.
        photonView.RPC(nameof(RPC_SubmitVote), RpcTarget.All, true);
    }

    public void OnClickReject()
    {
        photonView.RPC(nameof(RPC_SubmitVote), RpcTarget.All, false);
    }

    [PunRPC]
    private void RPC_SubmitVote(bool approved, PhotonMessageInfo info)
    {
        // Replace the previous vote from this player (allows mind-changing).
        _playerVotes[info.Sender.ActorNumber] = approved;

        // Recount from the dictionary so changed votes don't double-count.
        _votesApprove = 0;
        _votesReject  = 0;
        foreach (bool v in _playerVotes.Values)
        {
            if (v) _votesApprove++;
            else   _votesReject++;
        }

        OnPlayerVoted.Invoke(info.Sender.ActorNumber, approved);
    }

    // ── GM ends voting + advances turn ────────────────────────────────────────

    public void EndTurn()
    {
        if (!GameAuthority.IsGameMaster) return;

        // Capture counts before the reset clears them.
        int approveSnapshot = _votesApprove;
        int rejectSnapshot  = _votesReject;

        photonView.RPC(nameof(RPC_EndVoteSession), RpcTarget.All, approveSnapshot, rejectSnapshot);

        // Advance to the next player (master broadcasts internally via GameState).
        GameState.Instance.GMAdvanceTurn();
    }

    [PunRPC]
    private void RPC_EndVoteSession(int approveCount, int rejectCount)
    {
        bool approved = approveCount > rejectCount;
        Count(approved ? _accepted : _rejected, _pendingPlacerActorNumber);

        var pieceView = _pendingPiecePhotonViewId >= 0 ? PhotonView.Find(_pendingPiecePhotonViewId) : null;
        var votedInstrument = pieceView != null ? pieceView.GetComponent<InstrumentHook>()?.instrument : null;
        if (votedInstrument != null) _resultsByInstrument[votedInstrument.id] = (approveCount, rejectCount);
        if (pieceView != null)
        {
            var result = new VoteResult
            {
                approved = approved,
                yes = approveCount,
                no = rejectCount,
                votes = new List<bool>(_playerVotes.Values),
                instrumentName = pieceView.GetComponent<InstrumentHook>()?.instrument?.name ?? "The instrument",
                piece = pieceView.transform as RectTransform
            };
            OnVoteResult.Invoke(result);
        }

        // If rejected, send the instrument back to where it came from (after the result has played on it).
        if (!approved && pieceView != null)
            StartCoroutine(RevertAfterDelay(pieceView.GetComponent<DragPiece>(), _pendingOldCellIndex));

        // Log the outcome.
        string votedName = GameLog.B(pieceView ? pieceView.GetComponent<InstrumentHook>()?.instrument?.name ?? "The instrument" : "The instrument");
        string tally = $"{approveCount} yes · {rejectCount} no";
        if (approved)
            GameLog.Instance?.AddEntryLocal(GameLog.Kind.VotePassed, $"{votedName} approved", tally);
        else
            GameLog.Instance?.AddEntryLocal(GameLog.Kind.VoteFailed, $"{votedName} rejected, back to the tray", tally);

        // Accumulate per-player disagrees before the round data is cleared.
        foreach (var kvp in _playerVotes)
        {
            if (kvp.Value) continue; // voted approve — not a disagree
            if (!_cumulativeDisagrees.ContainsKey(kvp.Key))
                _cumulativeDisagrees[kvp.Key] = 0;
            _cumulativeDisagrees[kvp.Key]++;
        }

        ResetVote();
    }

    private IEnumerator RevertAfterDelay(DragPiece piece, int oldCellIndex)
    {
        if (rejectedReturnDelay > 0f) yield return new WaitForSecondsRealtime(rejectedReturnDelay);
        if (piece) piece.RevertToCell(oldCellIndex);
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    private void ResetVote()
    {
        _votesApprove             = 0;
        _votesReject              = 0;
        _playerVotes.Clear();
        _pendingPiecePhotonViewId = -1;
        _pendingPlacerActorNumber = -1;
        LocalCanVote = false;

        OnVoteEnded.Invoke();
    }
}
