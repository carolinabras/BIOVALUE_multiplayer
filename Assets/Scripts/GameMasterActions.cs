using Photon.Pun;
using TMPro;
using UnityEngine;

public class GameMasterActions : MonoBehaviour
{
    [SerializeField] public TMP_InputField playerName;
    [SerializeField] private EndGameManager endGameManager;

    public ChecksPlayer checksPlayer;

    public void EndInstrumentPhase()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        GameState.Instance.SetGamePhase(GameState.GamePhase.ActionCardPlay);
    }

    public void EndTurn()
    {
        VotingManager.Instance?.EndTurn();
    }

    public void EndActionsRound()
    {
        endGameManager?.OnClickOpenEndGame();
    }

    public void ShowResults()
    {
        endGameManager?.OnClickShowResultsToAll();
    }

    public void KickPlayer()
    {
        if (playerName.text != "")
        {
            PhotonView.Get(this).RPC(nameof(RPC_KickPlayer), RpcTarget.MasterClient, playerName.text);
            Debug.Log("Kicking player: " + playerName.text);
        }
    }

    [PunRPC]
    public void RPC_KickPlayer(string cardName)
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            string nameOnCard = "";
            if (player.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var n))
                nameOnCard = n as string ?? "";

            if (nameOnCard == cardName)
            {
                PhotonNetwork.CloseConnection(player);
                break;
            }
        }
    }
}
