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
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC(nameof(RPC_OpenEndGame), RpcTarget.All);
    }

    [PunRPC]
    private void RPC_OpenEndGame()
    {
        bool isGM = GameState.Instance.localPlayerIndex == 0;
        if (isGM) return; // GM has a separate results panel — nothing here
        endGamePanel.SetActive(true);
    }

    // Called by a separate GM-only button to open the results overview.
    public void OnClickOpenGMResults()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        gmResultsPanel.OpenResults();
    }
}
