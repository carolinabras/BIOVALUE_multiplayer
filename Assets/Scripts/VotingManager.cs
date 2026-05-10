using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class VotingManager : MonoBehaviourPun
{
    public static VotingManager Instance;

    [SerializeField] private GameObject votingPanel;

    // Shown on all clients after the GM ends the voting session.
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private float resultDisplaySeconds = 4f;

    // Optional: label showing the player's current vote selection.
    [SerializeField] private TMP_Text currentVoteLabel;

    private int _votesApprove = 0;
    private int _votesReject  = 0;
    private int _totalVoters  = 0;

    // Tracks each voter's latest vote so changing mind replaces, not stacks.
    private readonly Dictionary<int, bool> _playerVotes = new Dictionary<int, bool>();

    // Accumulated disagree (reject) vote count per actor across all voting rounds.
    private readonly Dictionary<int, int> _cumulativeDisagrees = new Dictionary<int, int>();

    public int GetDisagreeCount(int actorNumber) =>
        _cumulativeDisagrees.TryGetValue(actorNumber, out int c) ? c : 0;

    private int _pendingPiecePhotonViewId = -1;
    private int _pendingPlacerActorNumber = -1;
    private int _pendingOldCellIndex      = -1;

    // ── Events ────────────────────────────────────────────────────────────────
    public UnityEvent             OnVoteStarted = new UnityEvent();
    public UnityEvent<int, bool>  OnPlayerVoted = new UnityEvent<int, bool>();
    public UnityEvent             OnVoteEnded   = new UnityEvent();

    private void Awake()
    {
        Instance = this;
        votingPanel.SetActive(false);
        if (resultText) resultText.gameObject.SetActive(false);
    }

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

        OnVoteStarted.Invoke();

        if (resultText) resultText.gameObject.SetActive(false);

        if (!isGM && !isWhoPlaced)
        {
            votingPanel.SetActive(true);
            if (currentVoteLabel) currentVoteLabel.text = "";
        }

        string instrName  = piece?.GetComponent<InstrumentHook>()?.instrument?.name ?? "instrument";
        string placerName = GameLog.GetPlayerName(_pendingPlacerActorNumber);
        GameLog.Instance?.AddEntryLocal($"{placerName} placed \"{instrName}\" on the board");
    }

    // ── Player votes ──────────────────────────────────────────────────────────

    public void OnClickApprove()
    {
        // Panel stays open — player can still change their mind.
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

        // Show the local player's current selection.
        if (info.Sender.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber && currentVoteLabel)
            currentVoteLabel.text = approved ? "Your vote: Approve" : "Your vote: Reject";
    }

    // ── GM ends voting + advances turn ────────────────────────────────────────

    public void EndTurn()
    {
        if (!PhotonNetwork.IsMasterClient) return;

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
        votingPanel.SetActive(false);

        bool approved = approveCount > rejectCount;

        // If rejected, send the instrument back to where it came from.
        if (!approved)
        {
            var pieceView = PhotonView.Find(_pendingPiecePhotonViewId);
            pieceView?.GetComponent<DragPiece>()?.RevertToCell(_pendingOldCellIndex);
        }

        // Show result text on every client.
        if (resultText != null)
        {
            string verdict = approved ? "approved" : "disapproved";
            resultText.text = $"Instrument {verdict} — {approveCount} yes / {rejectCount} no";
            resultText.gameObject.SetActive(true);
            StartCoroutine(HideResultAfterDelay());
        }

        // Log the outcome.
        if (approveCount > rejectCount)
            GameLog.Instance?.AddEntryLocal($"Vote ended: instrument approved ({approveCount} yes, {rejectCount} no)");
        else
            GameLog.Instance?.AddEntryLocal($"Vote ended: instrument disapproved ({approveCount} yes, {rejectCount} no)");

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

    private IEnumerator HideResultAfterDelay()
    {
        yield return new WaitForSeconds(resultDisplaySeconds);
        if (resultText) resultText.gameObject.SetActive(false);
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    private void ResetVote()
    {
        _votesApprove             = 0;
        _votesReject              = 0;
        _playerVotes.Clear();
        _pendingPiecePhotonViewId = -1;
        _pendingPlacerActorNumber = -1;

        OnVoteEnded.Invoke();
    }
}
