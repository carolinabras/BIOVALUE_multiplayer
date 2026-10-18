using System.Collections;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    private const string GmQuitKey      = "gmQuit";     // the GM quit: players go back to the main menu
    private const int MainMenuScene     = 0;

    private bool _leaving;

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
            if (GameAuthority.IsGameMaster) OpenAllPlayersResults();
            else ShowEndGameForPlayers();
        }

        // Late-joiner: results were already broadcast — only show if GM explicitly sent them.
        if (GameAuthority.IsGameMaster &&
            props.TryGetValue(ShowResultsKey, out var sv) && sv is bool sb && sb)
            OpenAllPlayersResults();
    }

    // Called by the GM's "End Game" button — marks the room as finished for all clients.
    public void OnClickOpenEndGame()
    {
        if (!GameAuthority.IsGameMaster) return;
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { EndGameKey, true } });
    }

    // Called by the GM's "Show Results to All" button — opens the results panel on every client.
    public void OnClickShowResultsToAll()
    {
        if (!GameAuthority.IsGameMaster) return;
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { ShowResultsKey, true } });
    }

    // Fires on ALL clients when room properties change.
    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (changedProps.ContainsKey(EndGameKey))
        {
            ChatManager.Instance?.ClearChat();
            if (GameAuthority.IsGameMaster)
                OpenAllPlayersResults();
            else
                ShowEndGameForPlayers();
        }

        if (changedProps.ContainsKey(ShowResultsKey))
            OpenAllPlayersResults();

        if (changedProps.TryGetValue(GmQuitKey, out var q) && q is bool quit && quit && !GameAuthority.IsGameMaster)
            ReturnToMainMenu("The game master ended the game");
    }

    // The GM left without quitting (closed the game, lost the connection): Photon would make a player the
    // GM, so everyone goes back to the main menu instead.
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (!_leaving) ReturnToMainMenu("The game master left the game");
    }

    // Wired to the OpenResults button's OnClick in the Inspector — opens the panel locally.
    public void OpenEndGamePanel()
    {
        if (ResultsCard.Instance != null) return; // the results card replaces the old panels
        if (endGamePanel != null)
            endGamePanel.SetActive(true);
    }

    private void OpenAllPlayersResults()
    {
        if (ResultsCard.Instance != null) return; // the results card replaces the old panels
        if (allPlayersPanel != null)
            allPlayersPanel.SetActive(true);
        if (gmResultsPanel != null && allPlayersContainer != null)
            gmResultsPanel.SpawnInto(allPlayersContainer);
    }

    // "Quit game" (GM panel, results card). When the GM quits, the players are sent back to the main menu.
    public void QuitGame()
    {
        if (_leaving) return;
        _leaving = true;
        StartCoroutine(Quit());
    }

    private IEnumerator Quit()
    {
        if (GameAuthority.IsGameMaster && PhotonNetwork.InRoom)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { GmQuitKey, true } });
            yield return new WaitForSecondsRealtime(0.5f); // let the message reach the players first
        }
        PhotonNetwork.Disconnect();
        Application.Quit();
    }

    private void ReturnToMainMenu(string reason)
    {
        if (_leaving) return;
        _leaving = true;
        StartCoroutine(LeaveToMainMenu(reason));
    }

    // Same as leaving from the lobby: disconnect, wait for it, then load the main menu (it reconnects).
    private IEnumerator LeaveToMainMenu(string reason)
    {
        Debug.Log($"[EndGameManager] {reason}: back to the main menu.");
        if (GameNotice.Instance)
        {
            GameNotice.Instance.Show(reason, "Going back to the main menu…");
            yield return new WaitForSecondsRealtime(3f); // time to read it
        }
        PhotonNetwork.Disconnect();
        float timeout = 5f;
        while (PhotonNetwork.NetworkClientState != ClientState.Disconnected && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }
        SceneManager.LoadScene(MainMenuScene);
    }

    private void ShowEndGameForPlayers() { }
}
