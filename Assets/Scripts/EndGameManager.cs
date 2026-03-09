using Photon.Pun;
using UnityEngine;

public class EndGameManager : MonoBehaviourPun
{
    public static EndGameManager Instance;

    [SerializeField] private GameObject endGamePanel; // painel dos jogadores

    private void Awake()
    {
        Instance = this;
    }

    public void OnClickOpenEndGame()
    {
        photonView.RPC(nameof(RPC_OpenEndGame), RpcTarget.All);
    }

    [SerializeField] private GMResultsPanel gmResultsPanel;

    [PunRPC]
    private void RPC_OpenEndGame()
    {
        bool isGM = GameState.Instance.localPlayerIndex == 0;
        if (isGM)
            gmResultsPanel.OpenResults();
        else
            endGamePanel.SetActive(true);
    }
    
    
}
