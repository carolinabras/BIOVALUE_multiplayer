
using System.Linq;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class VotingManager : MonoBehaviourPun
{
    public static VotingManager Instance;

    [SerializeField] private GameObject votingPanel;

    private int _votesApprove = 0;
    private int _votesReject = 0;
    private int _totalVoters = 0;

    private int _pendingPiecePhotonViewId = -1;
    private int _pendingNewCellIndex = -1;
    private int _pendingOldCellIndex = -1;

    [SerializeField] private TMP_Text result;

    private void Awake()
    {
        Instance = this;
        votingPanel.SetActive(false);
    }


    // ── Start Vote ───────────────────────────────────────────────────────────

    public void StartVote(int piecePhotonViewId, int newCellIndex, int oldCellIndex)
    {
        photonView.RPC(nameof(RPC_StartVote), RpcTarget.All, piecePhotonViewId, newCellIndex, oldCellIndex);
    }

    [PunRPC]
    private void RPC_StartVote(int piecePhotonViewId, int newCellIndex, int oldCellIndex)
    {
        _votesApprove = 0;
        _votesReject = 0;
        _pendingPiecePhotonViewId = piecePhotonViewId;
        _pendingNewCellIndex = newCellIndex;
        _pendingOldCellIndex = oldCellIndex;

        // -1 GM, -1 quem colocou a peça
        _totalVoters = PhotonNetwork.PlayerList.Length - 2;

        bool isGM = GameState.Instance.localPlayerIndex == 0;
        var piece = PhotonView.Find(piecePhotonViewId);
        bool isWhoPlaced = piece != null && piece.IsMine;

        if (!isGM && !isWhoPlaced)
        {
            votingPanel.SetActive(true);
        }
    }


    // ── Buttons ──────────────────────────────────────────────────────────────

    public void OnClickApprove()
    {
        votingPanel.SetActive(false);
        photonView.RPC(nameof(RPC_SubmitVote), RpcTarget.All, true);
    }

    public void OnClickReject()
    {
        votingPanel.SetActive(false);
        photonView.RPC(nameof(RPC_SubmitVote), RpcTarget.All, false);
    }


    // ── Submit Vote ──────────────────────────────────────────────────────────

    [PunRPC]
    private void RPC_SubmitVote(bool approved)
    {
        if (approved) _votesApprove++;
        else _votesReject++;

        int totalVotes = _votesApprove + _votesReject;
        if (totalVotes < _totalVoters) return; // ainda faltam votos

        bool majorityApproves = _votesApprove > _votesReject;

        var piece = PhotonView.Find(_pendingPiecePhotonViewId);

        if (majorityApproves)
        {
            // owner e isPlaced já foram definidos pelo GiveOwner — nada a fazer
            Debug.Log($"Votação aprovada: {_votesApprove} vs {_votesReject}");
            result.text = "Instrument approved with " + _votesApprove + " votes!";
    }
        else
        {
            result.text = "Instrument rejected with " + _votesReject + " votes!";

            Debug.Log($"Votação rejeitada: {_votesApprove} vs {_votesReject}");

            if (piece != null)
            {
                // incrementa o rejected count do jogador que colocou
                int placerActorNumber = piece.Owner.ActorNumber;
                // converte ActorNumber para playerIndex
                var players = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToList();
                int placerIndex = players.FindIndex(p => p.ActorNumber == placerActorNumber);
                GameState.Instance.IncrementRejectedCount(placerIndex);
                
                
                // reverte owner e isPlaced
                var hook = piece.GetComponent<InstrumentHook>();
                if (hook?.instrument != null)
                {
                    hook.instrument.isPlaced = false;
                    hook.instrument.ownerId = -1;
                    InstrumentDatabaseSession.Instance.SessionDb.NotifyDatabaseChanged();
                }

                // reverte posição em todos os clientes
                piece.RPC(nameof(DragPiece.RPC_RevertToCell), RpcTarget.All, _pendingOldCellIndex);
            }
        }

        ResetVote();
    }


    // ── Reset ────────────────────────────────────────────────────────────────

    private void ResetVote()
    {
        _votesApprove = 0;
        _votesReject = 0;
        _pendingPiecePhotonViewId = -1;
        _pendingNewCellIndex = -1;
        _pendingOldCellIndex = -1;
    }
}