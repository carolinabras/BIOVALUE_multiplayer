using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class EndGameManager : MonoBehaviourPunCallbacks
{
    public static EndGameManager Instance;

    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private GMResultsPanel gmResultsPanel;
    [SerializeField] private GameObject allPlayersPanel;
    [SerializeField] private Transform allPlayersContainer;
    [SerializeField] private Button openResultsButton;

    private const string EndGameKey    = "endGame";
    private const string ShowResultsKey = "showResults";

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (!PhotonNetwork.InRoom) return;

        var props = PhotonNetwork.CurrentRoom.CustomProperties;

        // Late-joiner: end-game was already triggered.
        if (props.TryGetValue(EndGameKey, out var ev) && ev is bool eb && eb)
        {
            if (PhotonNetwork.IsMasterClient) OpenAllPlayersResults();
            else ShowEndGameForPlayers();
        }

        // Late-joiner: results were already broadcast — only show if GM explicitly sent them.
        if (PhotonNetwork.IsMasterClient &&
            props.TryGetValue(ShowResultsKey, out var sv) && sv is bool sb && sb)
            OpenAllPlayersResults();
    }

    // Called by the GM's "End Game" button — marks the room as finished for all clients.
    public void OnClickOpenEndGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { EndGameKey, true } });
    }

    // Called by the GM's "Show Results to All" button — opens the results panel on every client.
    public void OnClickShowResultsToAll()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { ShowResultsKey, true } });
    }

    // Fires on ALL clients when room properties change.
    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (changedProps.ContainsKey(EndGameKey))
        {
            ChatManager.Instance?.ClearChat();
            if (PhotonNetwork.IsMasterClient)
                OpenAllPlayersResults();
            else
                ShowEndGameForPlayers();
        }

        if (changedProps.ContainsKey(ShowResultsKey))
            OpenAllPlayersResults();
    }

    // Wired to the OpenResults button's OnClick in the Inspector — opens the panel locally.
    public void OpenEndGamePanel()
    {
        if (endGamePanel != null)
            endGamePanel.SetActive(true);
    }

    private void OpenAllPlayersResults()
    {
        if (allPlayersPanel != null)
            allPlayersPanel.SetActive(true);
        if (gmResultsPanel != null && allPlayersContainer != null)
            gmResultsPanel.SpawnInto(allPlayersContainer);
    }

    public void QuitGame()
    {
        PhotonNetwork.Disconnect();
        Application.Quit();
    }

    private void ShowEndGameForPlayers() { }
}
