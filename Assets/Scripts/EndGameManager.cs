using Photon.Pun;
using UnityEngine;

public class EndGameManager : MonoBehaviourPun
{
    public static EndGameManager Instance;

    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private GMResultsPanel gmResultsPanel;

    private void Awake()
    {
        Instance = this;
    }

    // Called by the GM's "End Actions Round" button.
    // Opens the FinalPlayerCard panel on every non-GM client.
    public void OnClickOpenEndGame()
    {
        photonView.RPC(nameof(RPC_OpenEndGame), RpcTarget.Others);
    }

    [PunRPC]
    private void RPC_OpenEndGame()
    {
        if (PhotonNetwork.IsMasterClient) return;
        if (endGamePanel != null) endGamePanel.SetActive(true);
    }

    // Called by a separate GM-only button to open the results overview.
    public void OnClickOpenGMResults()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        gmResultsPanel.OpenResults();
    }
}
