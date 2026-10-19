using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Main game, GM only: the GM drawer. A purple "GAME MASTER" tab on the left edge slides the panel in and
// out sideways (the tab travels with it), like the Game Log tab on the right.
// The panel: game phase (Instruments → Action cards → Collaboration → Results) with the button that moves to the next
// phase, "Show all actions", the players with their status and a remove button, and the results buttons.
// The phase and results buttons keep the scene wiring of the old GM panel's buttons (copied by the builder).
// Ending a turn lives in the turn ribbon. Built by the editor menu "BioValue/Build Turn Ribbon and GM Panel".
public class GameMasterHud : MonoBehaviourPunCallbacks
{
    public GameObject content;          // dim + drawer, hidden for players
    public RectTransform drawer;        // panel + tab; slides left until only the tab shows
    public RectTransform panel;
    public GameObject dim;              // darkens the board while open; a click on it closes
    public RectTransform tabArrow;      // chevron on the tab: points in when closed, out when open
    public float slideTime = 0.28f;
    public GameMasterActions actions;

    [Header("Phase")]
    public TMP_Text title;
    public Image[] stepDots;
    public TMP_Text[] stepMarks;
    public GameObject[] stepTicks;      // drawn tick on finished steps (Baloo has no tick glyph)
    public TMP_Text[] stepLabels;
    public GameObject endInstrumentsButton;
    public GameObject endActionsButton;     // starts the collaboration round
    public GameObject endCollabButton;      // wired like the old "End Actions Round" (results)
    public GameObject showActionsButton;    // "See everyone's actions · GM only", action cards round only

    [Header("Quit")]
    public GameObject quitButton;
    public GameObject quitConfirm;          // "Quit the game?" + Cancel / Quit

    [Header("Players")]
    public RectTransform rows;
    public GameObject rowTemplate;

    public bool IsOpen { get; private set; }

    private static readonly Color Purple = new Color32(0x46, 0x32, 0x7F, 0xFF);
    private static readonly Color Pink = new Color32(0xFE, 0x46, 0xA5, 0xFF);
    private static readonly Color PinkLight = new Color32(0xFF, 0xE3, 0xF1, 0xFF);
    private static readonly Color PinkDark = new Color32(0x8A, 0x14, 0x50, 0xFF);
    private static readonly Color Lilac = new Color32(0xEE, 0xEA, 0xF5, 0xFF);
    private static readonly Color Grey = new Color32(0x9A, 0x92, 0xAC, 0xFF);
    private static readonly Color Ink = new Color32(0x24, 0x1B, 0x3A, 0xFF);
    private static readonly Color Red = new Color32(0xD9, 0x3A, 0x3A, 0xFF);

    private const string EndGameKey = "endGame";
    private const float ConfirmSeconds = 3f;

    private readonly Dictionary<int, GameObject> rowByActor = new Dictionary<int, GameObject>();
    private int confirmActor = -1;
    private float confirmUntil;
    private float nextRefresh;

    private void Start()
    {
        if (rowTemplate) rowTemplate.SetActive(false);
        if (!actions) actions = FindFirstObjectByType<GameMasterActions>(FindObjectsInactive.Include);
        SetOpen(false, false);
        Refresh();
    }

    private void Update()
    {
        if (content && content.activeSelf != GameAuthority.IsGameMaster) content.SetActive(GameAuthority.IsGameMaster);
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { Toggle(); return; }
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.25f;
        Refresh();
    }

    public void Toggle() => SetOpen(!IsOpen, true);

    // Quit: asks first, in place of the button.
    public void AskQuit()
    {
        if (quitButton) quitButton.SetActive(false);
        if (quitConfirm) quitConfirm.SetActive(true);
    }

    public void CancelQuit()
    {
        if (quitButton) quitButton.SetActive(true);
        if (quitConfirm) quitConfirm.SetActive(false);
    }

    public void ConfirmQuit()
    {
        if (EndGameManager.Instance) EndGameManager.Instance.QuitGame();
        else
        {
            Photon.Pun.PhotonNetwork.Disconnect();
            if (WebBridge.IsWeb) UnityEngine.SceneManagement.SceneManager.LoadScene(0); // a browser tab can't quit
            else Application.Quit();
        }
    }

    // "See everyone's actions": closes this panel and opens the GM's actions drawer.
    public void OpenAllActions()
    {
        SetOpen(false, true);
        if (GmActionsDrawer.Instance) GmActionsDrawer.Instance.Show();
    }
    public void Close() => SetOpen(false, true);

    private void SetOpen(bool open, bool animate)
    {
        IsOpen = open;
        confirmActor = -1;
        CancelQuit();
        if (dim) dim.SetActive(open);
        if (open) Refresh();
        if (tabArrow) tabArrow.localRotation = Quaternion.Euler(0, 0, open ? 180f : 0f);
        if (!drawer || !panel) return;

        float x = open ? 0f : -drawer.rect.width; // closed: only the tab sticks out
        LeanTween.cancel(drawer.gameObject);
        var target = new Vector2(x, drawer.anchoredPosition.y);
        if (animate)
            LeanTween.value(drawer.gameObject, drawer.anchoredPosition, target, slideTime)
                .setOnUpdate((Vector2 v) => drawer.anchoredPosition = v)
                .setEaseOutCubic().setIgnoreTimeScale(true);
        else
            drawer.anchoredPosition = target;
    }

    // ── Content ──────────────────────────────────────────────────────────────

    private void Refresh()
    {
        var state = GameState.Instance;
        if (state == null) return;
        bool ended = PhotonNetwork.InRoom &&
                     PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(EndGameKey, out var e) && e is bool b && b;
        var phase = state.GetCurrentGamePhase();
        int step = ended ? 3 : phase == GameState.GamePhase.Collaboration ? 2 : phase == GameState.GamePhase.ActionCardPlay ? 1 : 0;

        if (title) title.text = step == 0 ? "Instruments round" : step == 1 ? "Action cards round" : step == 2 ? "Collaboration round" : "Results";
        int steps = stepDots != null ? stepDots.Length : 0;
        for (int i = 0; i < steps; i++)
        {
            if (stepDots != null && i < stepDots.Length && stepDots[i])
                stepDots[i].color = i == step ? Pink : i < step ? Purple : Lilac;
            if (stepMarks != null && i < stepMarks.Length && stepMarks[i])
            {
                stepMarks[i].text = i < step ? "" : (i + 1).ToString();
                stepMarks[i].color = i <= step ? Color.white : Grey;
            }
            if (stepTicks != null && i < stepTicks.Length && stepTicks[i])
                stepTicks[i].SetActive(i < step);
            if (stepLabels != null && i < stepLabels.Length && stepLabels[i])
            {
                stepLabels[i].color = i == step ? Ink : Grey;
                stepLabels[i].fontStyle = i == step ? FontStyles.Bold : FontStyles.Normal;
            }
        }
        if (endInstrumentsButton) endInstrumentsButton.SetActive(step == 0);
        if (endActionsButton) endActionsButton.SetActive(step == 1);
        if (endCollabButton) endCollabButton.SetActive(step == 2);
        if (showActionsButton) showActionsButton.SetActive(step == 1);

        RefreshRows(step == 0, step == 2, state.GetCurrentPlayerTurnIndex(), state.PeekNextTurnActor());
    }

    private void RefreshRows(bool placing, bool collaborating, int turn, int next)
    {
        if (!rows || !rowTemplate) return;
        var players = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).Skip(1).ToList(); // no GM

        foreach (var actor in rowByActor.Keys.ToList())
        {
            if (players.Any(p => p.ActorNumber == actor)) continue;
            Destroy(rowByActor[actor]);
            rowByActor.Remove(actor);
        }
        if (confirmActor >= 0 && Time.unscaledTime > confirmUntil) confirmActor = -1;

        for (int i = 0; i < players.Count; i++)
        {
            int actor = players[i].ActorNumber;
            if (!rowByActor.TryGetValue(actor, out var row))
            {
                row = Instantiate(rowTemplate, rows);
                row.SetActive(true);
                var kick = row.transform.Find("Kick")?.GetComponent<Button>();
                if (kick) kick.onClick.AddListener(() => OnKick(actor));
                rowByActor[actor] = row;
            }
            row.transform.SetSiblingIndex(i + 1); // after the hidden template

            int number = i + 1;
            Set(row, "Dot", img => img.color = PlayerCapsuleHook.ColorFor(number));
            SetText(row, "Dot/Number", number.ToString());
            SetText(row, "Name", GameLog.GetPlayerName(actor));

            bool confirming = confirmActor == actor;
            string status = placing && actor == turn ? "Placing" : placing && actor == next ? "Next"
                : collaborating && ColaborationManager.IsDone(players[i]) ? "Done" : "";
            var chip = row.transform.Find("Status");
            if (chip)
            {
                chip.gameObject.SetActive(status != "" && !confirming); // the wide "Remove?" takes its place
                chip.GetComponent<Image>().color = status == "Placing" ? PinkLight : Lilac;
                SetText(row, "Status/Label", status, status == "Placing" ? PinkDark : Purple);
            }

            Set(row, "Kick", img => img.color = confirming ? Red : new Color(1, 1, 1, 0));
            SetText(row, "Kick/Label", confirming ? "Remove?" : "×", confirming ? Color.white : Grey);
            var kickRT = row.transform.Find("Kick") as RectTransform;
            if (kickRT) kickRT.sizeDelta = new Vector2(confirming ? 92f : 34f, kickRT.sizeDelta.y);
        }
    }

    // First tap asks, second tap (within a few seconds) removes the player from the room.
    private void OnKick(int actor)
    {
        if (confirmActor == actor && Time.unscaledTime <= confirmUntil)
        {
            confirmActor = -1;
            if (actions) actions.KickActor(actor);
        }
        else
        {
            confirmActor = actor;
            confirmUntil = Time.unscaledTime + ConfirmSeconds;
        }
        Refresh();
    }

    private static void Set(GameObject row, string path, System.Action<Image> apply)
    {
        var t = row.transform.Find(path);
        var img = t ? t.GetComponent<Image>() : null;
        if (img) apply(img);
    }

    private static void SetText(GameObject row, string path, string text, Color? color = null)
    {
        var t = row.transform.Find(path);
        var tmp = t ? t.GetComponent<TMP_Text>() : null;
        if (!tmp) return;
        tmp.text = text;
        if (color.HasValue) tmp.color = color.Value;
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (IsOpen) Refresh();
    }
}
