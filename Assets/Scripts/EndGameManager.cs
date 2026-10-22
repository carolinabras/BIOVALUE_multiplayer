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

        // Rejoined while the GM is away.
        var gmNow = GameAuthority.GmPlayer;
        if (GameAuthority.GmActor > 0 && gmNow != null && gmNow.IsInactive) WaitForGm();

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

    // ── The GM dropped out ──────────────────────────────────────────────────
    // Photon makes a player the master client, but the GM stays the GM (GameAuthority): the players wait
    // for them to come back (RejoinGuard), then hand the master client back. If they don't come back in
    // time, or left for good, everyone goes back to the main menu.

    private const float GmGraceSeconds = 180f;
    private Coroutine _gmGrace;

    public override void OnPlayerLeftRoom(Player other)
    {
        if (_leaving || GameAuthority.GmActor < 0 || !GameAuthority.IsGm(other)) return;
        if (other.IsInactive) WaitForGm();
        else ReturnToMainMenu("The game master left the game");
    }

    public override void OnPlayerEnteredRoom(Player other)
    {
        if (_leaving || !GameAuthority.IsGm(other)) return;
        if (_gmGrace != null) StopCoroutine(_gmGrace);
        _gmGrace = null;
        if (GameNotice.Instance) GameNotice.Instance.ShowBrief("The game master is back", "The game carries on.");
        if (PhotonNetwork.IsMasterClient) PhotonNetwork.SetMasterClient(other);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (_leaving) return;
        if (GameAuthority.GmActor < 0) { ReturnToMainMenu("The game master left the game"); return; } // started without the Lobby
        // The GM is here (they came back before this client heard they had gone): give it back.
        var gm = GameAuthority.GmPlayer;
        if (newMasterClient.IsLocal && gm != null && !gm.IsInactive && !gm.IsLocal) PhotonNetwork.SetMasterClient(gm);
    }

    private void WaitForGm()
    {
        if (_gmGrace != null || GameAuthority.IsGameMaster) return;
        _gmGrace = StartCoroutine(GmGrace());
    }

    private IEnumerator GmGrace()
    {
        for (float left = GmGraceSeconds; left > 0f; left -= Time.unscaledDeltaTime)
        {
            var gm = GameAuthority.GmPlayer;
            if (gm != null && !gm.IsInactive) { _gmGrace = null; yield break; }
            if (GameNotice.Instance)
            {
                int s = Mathf.CeilToInt(left);
                string body = $"Waiting for them to come back… {s / 60}:{s % 60:00}";
                if (!GameNotice.Instance.content || !GameNotice.Instance.content.activeSelf)
                    GameNotice.Instance.Show("The game master lost connection", body);
                else if (GameNotice.Instance.body) GameNotice.Instance.body.text = body;
            }
            yield return null;
        }
        _gmGrace = null;
        ReturnToMainMenu("The game master didn't come back");
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
        if (WebBridge.IsWeb)
        {
            // A browser tab can't quit: back to the main menu instead.
            for (float t = 5f; PhotonNetwork.NetworkClientState != ClientState.Disconnected && t > 0f; t -= Time.unscaledDeltaTime)
                yield return null;
            SceneManager.LoadScene(MainMenuScene);
            yield break;
        }
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
