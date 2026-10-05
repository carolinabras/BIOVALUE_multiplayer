using ExitGames.Client.Photon;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class GameMasterActions : MonoBehaviour
{
    [SerializeField] public TMP_InputField playerName;
    [SerializeField] private EndGameManager endGameManager;
    [SerializeField] private GameObject endTurnButton;

    private void Start()
    {
        // The "End Instruments Round" button may change the phase directly via GameState,
        // so react to the phase itself rather than to EndInstrumentPhase() being called.
        GameState.Instance.onGamePhaseChanged.AddListener(OnGamePhaseChanged);
        var current = GameState.Instance.GetCurrentGamePhase();
        OnGamePhaseChanged(current != GameState.GamePhase.None ? current : GameState.GamePhase.InstrumentSelection);
    }

    private void OnDestroy()
    {
        if (GameState.Instance) GameState.Instance.onGamePhaseChanged.RemoveListener(OnGamePhaseChanged);
    }

    private void OnGamePhaseChanged(GameState.GamePhase gamePhase)
    {
        if (endTurnButton) endTurnButton.SetActive(gamePhase == GameState.GamePhase.InstrumentSelection);
    }

    public void EndInstrumentPhase()
    {
        if (!GameAuthority.IsGameMaster) return;
        GameState.Instance.SetGamePhase(GameState.GamePhase.ActionCardPlay);
        endTurnButton?.SetActive(false);
    }

    public void EndTurn()
    {
        VotingManager.Instance?.EndTurn();
    }

    public void EndActionsRound()
    {
        if (!GameAuthority.IsGameMaster) return;
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { "actionsRoundEnded", true } });
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
    public void RPC_KickPlayer(string cardName, PhotonMessageInfo info)
    {
        if (!GameAuthority.SenderIsGameMaster(info)) return;

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
