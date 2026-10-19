using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// The Lobby (mockup "Lobby: create or join, GM set-up, player waiting"):
//  1. Create or join: "I'm the game master" (room name → Create room) or "I'm a player" (your name +
//     room name → Join room). Wrong names / taken names are explained under the field.
//  2. Game master: set up in 4 steps (Game · Instruments · Action cards · Scoring) and see who has joined.
//     "Start the game" waits for the objective, enough instruments (2 per player) and every player.
//  3. Player: "You're in!" and who is in the room, until the game master starts.
// Replaces CreateJoin / UI_LobbyData / LobbyUI on the old panels. Built by "BioValue/Build Lobby".
public class LobbyScreen : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    public class ScoreRow
    {
        public Button toggle;
        public Image track;
        public RectTransform knob;
        public GameObject stepper;
        public Button minus, plus;
        public TMP_Text weight;
    }

    private const string RoleKey = "role"; // "GM" | "Player", same as CreateJoin
    private const int MainMenuScene = 0;
    private const string NextScene = "Role";
    private const float WeightStep = 1f, MaxWeight = 10f;

    private static readonly Color Pink = new Color32(0xFE, 0x49, 0xA4, 0xFF);
    private static readonly Color Purple = new Color32(0x54, 0x34, 0xC8, 0xFF);
    private static readonly Color Ink = new Color32(0x24, 0x1B, 0x3A, 0xFF);
    private static readonly Color Muted = new Color32(0x6B, 0x65, 0x84, 0xFF);
    private static readonly Color Line = new Color32(0xDD, 0xD6, 0xEA, 0xFF);
    private static readonly Color Lilac = new Color32(0xF6, 0xF2, 0xFC, 0xFF);
    private static readonly Color Disabled = new Color32(0xF1, 0xEF, 0xF6, 0xFF);
    private static readonly Color Faint = new Color32(0xA9, 0xA3, 0xBC, 0xFF);
    private static readonly Color OkBg = new Color32(0xDD, 0xF3, 0xE0, 0xFF);
    private static readonly Color OkText = new Color32(0x1E, 0x7A, 0x2C, 0xFF);
    private static readonly Color PinkBg = new Color32(0xFF, 0xE3, 0xF1, 0xFF);
    private static readonly Color PinkText = new Color32(0x8A, 0x14, 0x50, 0xFF);
    private static readonly Color PinkTile = new Color32(0xFF, 0xF6, 0xFA, 0xFF);
    private static readonly Color Error = new Color32(0xC0, 0x26, 0x3E, 0xFF);
    private static readonly Color Body = new Color32(0x4A, 0x44, 0x60, 0xFF);

    // Category colours (ring, text), as on the instrument tray. Index = InstrumentType.
    private static readonly Color[] TypeRing =
    {
        new Color32(0xA9, 0xA3, 0xBC, 0xFF), new Color32(0xFF, 0xC0, 0x00, 0xFF), new Color32(0x40, 0x40, 0x40, 0xFF),
        new Color32(0xFF, 0xB0, 0x11, 0xFF), new Color32(0x47, 0xD4, 0x5A, 0xFF), new Color32(0xC0, 0x00, 0x00, 0xFF),
        new Color32(0x22, 0x8A, 0xA2, 0xFF)
    };
    private static readonly Color[] TypeText =
    {
        new Color32(0x6B, 0x65, 0x84, 0xFF), new Color32(0x8A, 0x6A, 0x00, 0xFF), new Color32(0x40, 0x40, 0x40, 0xFF),
        new Color32(0x9A, 0x5B, 0x00, 0xFF), new Color32(0x1E, 0x7A, 0x2C, 0xFF), new Color32(0xC0, 0x00, 0x00, 0xFF),
        new Color32(0x17, 0x68, 0x7A, 0xFF)
    };
    private static readonly Color[] TypeFill =
    {
        new Color32(0xEE, 0xEA, 0xF5, 0xFF), new Color32(0xFA, 0xF7, 0xD2, 0xFF), new Color32(0xE8, 0xE8, 0xE8, 0xFF),
        new Color32(0xF9, 0xE5, 0xCB, 0xFF), new Color32(0xD9, 0xF2, 0xD0, 0xFF), new Color32(0xFC, 0xD4, 0xD4, 0xFF),
        new Color32(0xD0, 0xEA, 0xF0, 0xFF)
    };
    // Filter / new-instrument order: Planning, Information, Incentive, Agreement, Regulation, Enforcement.
    public static readonly InstrumentType[] TypeOrder =
    {
        InstrumentType.Planning, InstrumentType.Information, InstrumentType.Incentive,
        InstrumentType.Agreement, InstrumentType.Regulation, InstrumentType.Inspection
    };
    public static readonly int[] Counts = { 2, 3, 4, 5, 6, 7, 8 };
    public static readonly int[] Limits = { 1, 2, 3, 4, 5, 0 };

    [Header("Screens")]
    public GameObject startScreen;
    public GameObject setupScreen;
    public GameObject waitingScreen;

    [Header("Top bar")]
    public GameObject roomPill;
    public TMP_Text roomLabel;
    public TMP_Text copyLabel;
    public TMP_Text leaveLabel;

    [Header("1 · Create or join")]
    public TMP_InputField createName;
    public Button createButton;
    public TMP_Text createHint;
    public TMP_InputField playerName;
    public TMP_InputField joinName;
    public Button joinButton;
    public TMP_Text joinHint;

    [Header("2 · Players (game master)")]
    public TMP_Text playersCount;
    public TMP_Text playersHint;
    public RectTransform playerList;
    public GameObject playerSlotTemplate;

    [Header("2 · Steps")]
    public Button[] tabButtons;
    public GameObject[] tabPages;

    [Header("Game")]
    public Button[] countButtons;
    public TMP_InputField objective;
    public TMP_InputField link;

    [Header("Instruments")]
    public TMP_Text instHint;
    public TMP_Text instCount;
    public Button[] filterButtons; // All, then TypeOrder
    public RectTransform instGrid;
    public GameObject instTileTemplate;
    public Button addInstrumentButton;
    public Button[] viewButtons;          // Cards, List with descriptions
    public GameObject instCardsView;
    public GameObject instListView;
    public RectTransform instList;
    public GameObject instRowTemplate;
    public GameObject addDialog;
    public TMP_InputField newName;
    public TMP_InputField newDescription;
    public Button[] newTypeButtons; // TypeOrder
    public Button addConfirm;

    [Header("Instrument details (the i on a tile)")]
    public GameObject detailsDialog;
    public RectTransform detailsCard;
    public Image detailsLeft;
    public Image detailsRing;
    public TMP_Text detailsNumber;
    public GameObject[] detailsCategories; // chips: Dot, Label
    public TMP_Text detailsPosition;
    public TMP_Text detailsName;
    public GameObject detailsSelected;
    public TMP_Text detailsDescription;
    public ScrollRect detailsScroll;
    public Button detailsPrev;
    public Button detailsNext;
    public Button detailsSelect;
    public Button[] detailsClose;          // backdrop, ×

    [Header("Action cards")]
    public Button[] limitButtons;
    public TMP_Text limitText;

    [Header("Scoring")]
    public TMP_Text formulaText;
    public TMP_Text formulaTag;
    public ScoreRow[] scoreRows; // ScoreFormula.Choosable
    public GameObject combineRow;
    public Button multiplyButton;
    public Button addButton;
    public Button defaultButton;

    [Header("Footer")]
    public GameObject[] checks; // objective, instruments, players
    public Button backButton;
    public Button nextButton;
    public Button startButton;

    [Header("3 · Waiting")]
    public TMP_Text waitingCount;
    public RectTransform waitingList;
    public GameObject waitingChipTemplate;
    public TMP_Text waitingTitle;
    public TMP_Text waitingSub;

    private InstrumentsDatabase _db;
    private int _tab, _players = 4, _limit = 3, _filter = -1, _newType;
    private ScoreFormula.Setup _formula = ScoreFormula.DefaultSetup();
    private bool _leaving, _starting, _listView;
    private Instrument _details;
    private readonly List<GameObject> _rows = new List<GameObject>();
    private string _pendingRoom = "";
    private readonly List<GameObject> _tiles = new List<GameObject>();
    private readonly List<GameObject> _slots = new List<GameObject>();
    private readonly List<GameObject> _chips = new List<GameObject>();

    private bool IsGm => PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient;

    // ── Setup ────────────────────────────────────────────────────────────────

    private void Start()
    {
        _db = InstrumentDatabaseSession.Instance ? InstrumentDatabaseSession.Instance.SessionDb : null;
        PhotonNetwork.AutomaticallySyncScene = true;
        if (!PhotonNetwork.IsConnected) PhotonNetwork.ConnectUsingSettings();

        createName.onValueChanged.AddListener(_ => { SetHint(createHint, "Your players type this name to join.", false); RefreshStart(); });
        joinName.onValueChanged.AddListener(_ => { SetHint(joinHint, "Capital letters don't matter.", false); RefreshStart(); });
        playerName.onValueChanged.AddListener(_ => RefreshStart());
        createButton.onClick.AddListener(CreateRoom);
        joinButton.onClick.AddListener(JoinRoom);

        for (int i = 0; i < tabButtons.Length; i++) { int t = i; tabButtons[i].onClick.AddListener(() => ShowTab(t)); }
        backButton.onClick.AddListener(() => ShowTab(_tab - 1));
        nextButton.onClick.AddListener(() => ShowTab(_tab + 1));
        startButton.onClick.AddListener(StartGame);

        for (int i = 0; i < countButtons.Length; i++) { int n = Counts[i]; countButtons[i].onClick.AddListener(() => SetPlayers(n)); }
        objective.onValueChanged.AddListener(_ => RefreshSetup());
        for (int i = 0; i < filterButtons.Length; i++) { int f = i - 1; filterButtons[i].onClick.AddListener(() => { _filter = f; RefreshInstruments(); }); }
        addInstrumentButton.onClick.AddListener(OpenAddDialog);
        for (int i = 0; i < newTypeButtons.Length; i++) { int t = i; newTypeButtons[i].onClick.AddListener(() => { _newType = t; RefreshAddDialog(); }); }
        newName.onValueChanged.AddListener(_ => RefreshAddDialog());
        addConfirm.onClick.AddListener(AddInstrument);
        for (int i = 0; i < viewButtons.Length; i++) { bool list = i == 1; viewButtons[i].onClick.AddListener(() => { _listView = list; RefreshInstruments(); }); }
        foreach (var b in detailsClose) b.onClick.AddListener(CloseDetails);
        detailsPrev.onClick.AddListener(() => StepDetails(-1));
        detailsNext.onClick.AddListener(() => StepDetails(1));
        detailsSelect.onClick.AddListener(() =>
        {
            if (_details == null) return;
            _details.isSelected = !_details.isSelected;
            RefreshSetup();
        });

        for (int i = 0; i < limitButtons.Length; i++) { int n = Limits[i]; limitButtons[i].onClick.AddListener(() => { _limit = n; RefreshCards(); }); }

        for (int i = 0; i < scoreRows.Length; i++)
        {
            var item = ScoreFormula.Choosable[i];
            scoreRows[i].toggle.onClick.AddListener(() => ToggleItem(item));
            scoreRows[i].minus.onClick.AddListener(() => Nudge(item, -WeightStep));
            scoreRows[i].plus.onClick.AddListener(() => Nudge(item, WeightStep));
        }
        multiplyButton.onClick.AddListener(() => { _formula.multiply = true; RefreshScoring(); });
        addButton.onClick.AddListener(() => { _formula.multiply = false; RefreshScoring(); });
        defaultButton.onClick.AddListener(() => { _formula = ScoreFormula.DefaultSetup(); RefreshScoring(); });

        playerSlotTemplate.SetActive(false);
        waitingChipTemplate.SetActive(false);
        instTileTemplate.SetActive(false);
        instRowTemplate.SetActive(false);
        addDialog.SetActive(false);
        detailsDialog.SetActive(false);

        BuildTiles();
        ShowScreen(PhotonNetwork.InRoom ? (IsGm ? setupScreen : waitingScreen) : startScreen);
    }

    // ── Screens ──────────────────────────────────────────────────────────────

    private void ShowScreen(GameObject screen)
    {
        startScreen.SetActive(screen == startScreen);
        setupScreen.SetActive(screen == setupScreen);
        waitingScreen.SetActive(screen == waitingScreen);
        bool inRoom = screen != startScreen;
        roomPill.SetActive(inRoom);
        if (inRoom && PhotonNetwork.InRoom) roomLabel.text = PhotonNetwork.CurrentRoom.Name;
        leaveLabel.text = inRoom ? "Leave room" : "Main menu";
        if (screen == startScreen) RefreshStart();
        if (screen == setupScreen) { ShowTab(_tab); PushPlayers(); }
        if (screen == waitingScreen) RefreshWaiting();
    }

    // Top right: "Main menu" before joining, "Leave room" after.
    public void Leave()
    {
        if (_leaving) return;
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            ShowScreen(startScreen);
            return;
        }
        _leaving = true;
        StartCoroutine(BackToMenu());
    }

    private IEnumerator BackToMenu()
    {
        PhotonNetwork.AutomaticallySyncScene = false; // so Photon can't redirect the next scene load
        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();
        for (float t = 5f; PhotonNetwork.NetworkClientState != ClientState.Disconnected && t > 0f; t -= Time.unscaledDeltaTime)
            yield return null;
        SceneManager.LoadScene(MainMenuScene);
    }

    public void CopyRoomName()
    {
        if (!PhotonNetwork.InRoom) return;
        WebBridge.CopyText(PhotonNetwork.CurrentRoom.Name); // the browser clipboard in the web build
        StopCoroutine(nameof(Copied));
        StartCoroutine(nameof(Copied));
    }

    private IEnumerator Copied()
    {
        copyLabel.text = "Copied";
        yield return new WaitForSecondsRealtime(1.4f);
        copyLabel.text = "Copy";
    }

    // ── 1 · Create or join ───────────────────────────────────────────────────

    private static string RoomName(string raw) => (raw ?? "").Trim().ToUpperInvariant();

    private bool OnMaster => PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer ||
                             PhotonNetwork.NetworkClientState == ClientState.JoinedLobby;

    private void RefreshStart()
    {
        bool canCreate = RoomName(createName.text).Length > 0;
        bool canJoin = RoomName(joinName.text).Length > 0 && playerName.text.Trim().Length > 0;
        Fill(createButton, canCreate ? Pink : Disabled, canCreate ? Color.white : Faint);
        Fill(joinButton, canJoin ? new Color32(0x25, 0x7A, 0xFE, 0xFF) : Disabled, canJoin ? Color.white : Faint);
    }

    private void CreateRoom()
    {
        string room = RoomName(createName.text);
        if (room.Length == 0) return;
        if (!OnMaster) { SetHint(createHint, "Still connecting to the server… try again in a moment.", true); return; }
        _pendingRoom = room;
        var options = new RoomOptions
        {
            IsOpen = true,
            IsVisible = false,
            PlayerTtl = 60000,    // a dropped player can come back for 60 s
            EmptyRoomTtl = 60000,
        };
        PhotonNetwork.CreateRoom(room, options);
    }

    private void JoinRoom()
    {
        string room = RoomName(joinName.text);
        string me = playerName.text.Trim();
        if (room.Length == 0 || me.Length == 0) return;
        if (!OnMaster) { SetHint(joinHint, "Still connecting to the server… try again in a moment.", true); return; }
        _pendingRoom = room;
        // Sent with the join; the Role scene shows it in the name field.
        PhotonNetwork.NickName = me;
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { BiovalueStatics.PlayerNameKey, me } });
        PhotonNetwork.JoinRoom(room);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[Lobby] CreateRoom failed ({returnCode}): {message}");
        SetHint(createHint, returnCode == ErrorCode.GameIdAlreadyExists
            ? $"There's already a room called “{_pendingRoom}”. Pick another name."
            : "Couldn't create the room. Try again.", true);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[Lobby] JoinRoom failed ({returnCode}): {message}");
        SetHint(joinHint, returnCode switch
        {
            ErrorCode.GameDoesNotExist => $"No room called “{_pendingRoom}”. Check the name with your game master.",
            ErrorCode.GameClosed => "This game has already started.",
            ErrorCode.GameFull => "This room is full.",
            _ => "Couldn't join the room. Try again."
        }, true);
    }

    public override void OnJoinedRoom()
    {
        bool gm = PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 1;
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { RoleKey, gm ? "GM" : "Player" } });
        Debug.Log($"[Lobby] Joined {PhotonNetwork.CurrentRoom.Name} as {(gm ? "GM" : "Player")}.");
        ShowScreen(gm ? setupScreen : waitingScreen);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) => RefreshPeople();
    public override void OnPlayerLeftRoom(Player otherPlayer) => RefreshPeople();
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) => RefreshPeople();
    public override void OnRoomPropertiesUpdate(Hashtable changed) => RefreshPeople();

    // The game master left before starting: nobody can start the game, so the players leave too.
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (!PhotonNetwork.InRoom || _starting) return;
        PhotonNetwork.LeaveRoom();
        ShowScreen(startScreen);
        SetHint(joinHint, "The game master left the room.", true);
    }

    private void RefreshPeople()
    {
        if (setupScreen.activeSelf) { RefreshPlayers(); RefreshFooter(); }
        if (waitingScreen.activeSelf) RefreshWaiting();
    }

    // ── 2 · Game master ──────────────────────────────────────────────────────

    // Dropped players (kept for 60 s so they can rejoin) haven't joined.
    private static Player[] Joined() => ScoreFormula.Players().Where(p => !p.IsInactive).ToArray();

    private static string NameOf(Player p) =>
        p.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var n) && n is string s && s.Trim().Length > 0
            ? s.Trim() : "Player " + TurnRibbon.PlayerNumberOf(p.ActorNumber);

    private void SetPlayers(int n)
    {
        _players = n;
        PushPlayers();
        RefreshSetup();
    }

    // The players see "2 of 4 players" while they wait.
    private void PushPlayers()
    {
        if (IsGm) PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { BiovalueStatics.LobbyPlayersKey, _players } });
    }

    private void ShowTab(int tab)
    {
        _tab = Mathf.Clamp(tab, 0, tabPages.Length - 1);
        for (int i = 0; i < tabPages.Length; i++) tabPages[i].SetActive(i == _tab);
        RefreshSetup();
    }

    private bool ObjectiveOk => objective.text.Trim().Length > 0;
    private int SelectedCount => _db ? _db.instruments.Count(i => i.isSelected) : 0;
    private bool InstrumentsOk => SelectedCount >= _players * 2;
    private bool PlayersOk => Joined().Length >= _players;

    private void RefreshSetup()
    {
        bool[] done = { ObjectiveOk, InstrumentsOk, true, true };
        for (int i = 0; i < tabButtons.Length; i++)
        {
            bool on = i == _tab;
            var t = tabButtons[i].transform;
            SetColor(t, "Underline", on ? Pink : Color.clear);
            SetColor(t, "Label", on ? Ink : Muted);
            SetColor(t, "Badge", done[i] ? OkBg : on ? Pink : Disabled);
            SetColor(t, "Badge/Number", on ? Color.white : Muted);
            SetActive(t, "Badge/Number", !done[i]);
            SetActive(t, "Badge/Tick", done[i]);
        }
        for (int i = 0; i < countButtons.Length; i++) Chip(countButtons[i], Counts[i] == _players);
        RefreshPlayers();
        RefreshInstruments();
        RefreshCards();
        RefreshScoring();
        RefreshFooter();
    }

    private void RefreshPlayers()
    {
        var joined = Joined();
        int shown = Mathf.Min(joined.Length, _players);
        playersCount.text = $"{shown} of {_players} joined";
        playersCount.color = joined.Length >= _players ? OkText : Muted;
        if (PhotonNetwork.InRoom) playersHint.text = $"They join with the room name <b><color=#241B3A>{PhotonNetwork.CurrentRoom.Name}</color></b>.";

        int count = Mathf.Max(_players, joined.Length);
        while (_slots.Count < count) _slots.Add(Instantiate(playerSlotTemplate, playerList));
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            slot.SetActive(i < count);
            if (i >= count) continue;
            bool here = i < joined.Length;
            var t = slot.transform;
            SetColor(t, "", here ? Lilac : Color.white);
            SetColor(t, "Border", here ? Lilac : Line);
            SetColor(t, "Hex", here ? PlayerCapsuleHook.ColorFor(TurnRibbon.PlayerNumberOf(joined[i].ActorNumber)) : Disabled);
            SetText(t, "Name", here ? NameOf(joined[i]) : "Player " + (i + 1), here ? Ink : Faint);
            SetText(t, "Tag", here ? (i < _players ? "Joined" : "Extra") : "Waiting…", here ? OkText : Faint);
        }
    }

    // Instruments: one tile (cards view) and one row (list view) per instrument in the session database.
    private void BuildTiles()
    {
        foreach (var t in _tiles) Destroy(t);
        foreach (var r in _rows) Destroy(r);
        _tiles.Clear();
        _rows.Clear();
        if (!_db) return;
        foreach (var inst in _db.instruments)
        {
            var captured = inst;
            UnityEngine.Events.UnityAction toggle = () =>
            {
                captured.isSelected = !captured.isSelected;
                RefreshSetup();
            };
            int type = Mathf.Clamp((int)inst.typeOne, 0, TypeRing.Length - 1);

            var tile = Instantiate(instTileTemplate, instGrid);
            tile.name = "Instrument " + inst.id;
            tile.GetComponent<Button>().onClick.AddListener(toggle);
            var info = tile.transform.Find("Info");
            if (info) info.GetComponent<Button>().onClick.AddListener(() => OpenDetails(captured));
            SetColor(tile.transform, "Bar", TypeRing[type]);
            SetText(tile.transform, "Type", inst.typeOne == InstrumentType.None ? "Other" : inst.typeOne.DisplayName(), TypeText[type]);
            SetText(tile.transform, "Name", inst.name, Ink);
            _tiles.Add(tile);

            var row = Instantiate(instRowTemplate, instList);
            row.name = "Instrument " + inst.id;
            row.GetComponent<Button>().onClick.AddListener(toggle);
            SetColor(row.transform, "Bar", TypeRing[type]);
            string categories = string.Join("<color=#A9A3BC>  ·  </color>",
                TypesOf(inst).Select(t => $"<color=#{ColorUtility.ToHtmlStringRGB(TypeText[(int)t])}>{t.DisplayName()}</color>"));
            SetText(row.transform, "Head", inst.name + (categories.Length > 0 ? "   <size=75%>" + categories + "</size>" : ""), Ink);
            bool described = !string.IsNullOrWhiteSpace(inst.description);
            SetText(row.transform, "Description", described ? inst.description : "No description yet.", described ? Body : Faint);
            _rows.Add(row);
        }
        addInstrumentButton.transform.SetAsLastSibling();
    }

    // The instrument's categories (one or two), in order.
    private static List<InstrumentType> TypesOf(Instrument inst)
    {
        var types = new List<InstrumentType>();
        if (inst.typeOne != InstrumentType.None) types.Add(inst.typeOne);
        if (inst.typeTwo != InstrumentType.None && inst.typeTwo != inst.typeOne) types.Add(inst.typeTwo);
        return types;
    }

    private bool Shown(Instrument inst) => _filter < 0 || inst.typeOne == TypeOrder[_filter];

    // Selected: pink border and fill, ticked check box. Same for tiles and rows.
    private static void LookSelected(Transform t, bool on)
    {
        SetColor(t, "", on ? Pink : Line);
        SetColor(t, "Fill", on ? PinkTile : Color.white);
        SetColor(t, "Check", on ? Pink : Line);
        SetColor(t, "Check/Fill", on ? Pink : Color.white);
        SetActive(t, "Check/Tick", on);
    }

    private void RefreshInstruments()
    {
        int min = _players * 2, count = SelectedCount;
        instHint.text = _listView
            ? $"At least {min}: 2 for each player. Click an instrument to select it."
            : $"At least {min}: 2 for each player. Click to select · i for the details.";
        instCardsView.SetActive(!_listView);
        instListView.SetActive(_listView);
        for (int i = 0; i < viewButtons.Length; i++) Segment(viewButtons[i], (i == 1) == _listView);
        instCount.text = count + " selected";
        instCount.color = count >= min ? OkText : PinkText;
        SetColor(instCount.transform.parent, "", count >= min ? OkBg : PinkBg);

        for (int i = 0; i < filterButtons.Length; i++)
        {
            bool on = i - 1 == _filter;
            var t = filterButtons[i].transform;
            SetColor(t, "", on ? Ink : Line);
            SetColor(t, "Fill", on ? Ink : Color.white);
            SetColor(t, "Label", on ? Color.white : Ink);
        }
        if (!_db) return;
        for (int i = 0; i < _tiles.Count && i < _db.instruments.Count; i++)
        {
            var inst = _db.instruments[i];
            bool show = Shown(inst);
            _tiles[i].SetActive(show);
            if (i < _rows.Count) _rows[i].SetActive(show);
            if (!show) continue;
            LookSelected(_tiles[i].transform, inst.isSelected);
            if (i < _rows.Count) LookSelected(_rows[i].transform, inst.isSelected);
        }
        addInstrumentButton.gameObject.SetActive(_filter < 0);
        if (detailsDialog.activeSelf) RefreshDetails(false);
    }

    // ── Instrument details ───────────────────────────────────────────────────

    private void Update()
    {
        if (detailsDialog && detailsDialog.activeSelf && Input.GetKeyDown(KeyCode.Escape)) CloseDetails();
    }

    private void OpenDetails(Instrument inst)
    {
        _details = inst;
        detailsDialog.SetActive(true);
        detailsDialog.transform.SetAsLastSibling();
        RefreshDetails(true);
        LeanTween.cancel(detailsCard.gameObject);
        detailsCard.localScale = Vector3.one * 0.92f;
        LeanTween.scale(detailsCard.gameObject, Vector3.one, 0.22f).setEaseOutBack();
    }

    private void CloseDetails()
    {
        detailsDialog.SetActive(false);
        _details = null;
    }

    // The arrows browse the instruments shown by the current filter.
    private void StepDetails(int by)
    {
        if (!_db) return;
        var shown = _db.instruments.Where(Shown).ToList();
        if (shown.Count == 0) return;
        int at = shown.IndexOf(_details);
        _details = shown[at < 0 ? 0 : (at + by + shown.Count) % shown.Count];
        RefreshDetails(true);
    }

    private void RefreshDetails(bool fresh)
    {
        var inst = _details;
        if (inst == null || !_db) return;
        int type = Mathf.Clamp((int)inst.typeOne, 0, TypeRing.Length - 1);
        detailsLeft.color = TypeFill[type];
        detailsRing.color = TypeRing[type];
        detailsNumber.text = inst.id.ToString();
        var types = TypesOf(inst);
        for (int i = 0; i < detailsCategories.Length; i++)
        {
            bool on = i < types.Count;
            detailsCategories[i].SetActive(on);
            if (!on) continue;
            int t = (int)types[i];
            SetColor(detailsCategories[i].transform, "Dot", TypeRing[t]);
            SetText(detailsCategories[i].transform, "Label", types[i].DisplayName(), TypeText[t]);
        }

        var shown = _db.instruments.Where(Shown).ToList();
        int at = shown.IndexOf(inst);
        detailsPosition.text = at < 0 ? "INSTRUMENT" : $"INSTRUMENT · {at + 1} OF {shown.Count}";
        detailsName.text = inst.name;
        detailsSelected.SetActive(inst.isSelected);
        bool described = !string.IsNullOrWhiteSpace(inst.description);
        detailsDescription.text = described ? inst.description : "No description yet.";
        detailsDescription.color = described ? Body : Faint;
        if (fresh) detailsScroll.verticalNormalizedPosition = 1f;
        detailsPrev.interactable = detailsNext.interactable = shown.Count > 1;

        Fill(detailsSelect, inst.isSelected ? Disabled : Pink, inst.isSelected ? Ink : Color.white);
        SetText(detailsSelect.transform, "Label", inst.isSelected ? "Remove from the game" : "Select for this game",
                inst.isSelected ? Ink : Color.white);
    }

    private void OpenAddDialog()
    {
        newName.text = "";
        newDescription.text = "";
        _newType = 0;
        addDialog.SetActive(true);
        addDialog.transform.SetAsLastSibling();
        RefreshAddDialog();
    }

    public void CloseAddDialog() => addDialog.SetActive(false);

    private void RefreshAddDialog()
    {
        for (int i = 0; i < newTypeButtons.Length; i++) Chip(newTypeButtons[i], i == _newType);
        bool ok = newName.text.Trim().Length > 0;
        Fill(addConfirm, ok ? Pink : Disabled, ok ? Color.white : Faint);
    }

    private void AddInstrument()
    {
        string name = newName.text.Trim();
        if (name.Length == 0 || !_db) return;
        int id;
        do id = Random.Range(100, 1000); while (_db.instruments.Exists(i => i.id == id));
        string description = newDescription.text.Trim();
        _db.AddInstrument(new Instrument
        {
            id = id,
            name = name,
            description = description,
            generalDescription = description,
            typeOne = TypeOrder[_newType],
            isSelected = true, // they just made it for this game
        });
        CloseAddDialog();
        BuildTiles();
        RefreshSetup();
    }

    private void RefreshCards()
    {
        for (int i = 0; i < limitButtons.Length; i++) Chip(limitButtons[i], Limits[i] == _limit);
        limitText.text = _limit > 0
            ? $"Each player can play up to {_limit} action card{(_limit > 1 ? "s" : "")} per round. The card fan shows how many are left."
            : "Players can play as many action cards as they like each round.";
    }

    // Scoring: switch items on, set their points (− takes points away), multiply or add the two averages.
    private void ToggleItem(ScoreFormula.Item item)
    {
        if (_formula.weights.ContainsKey(item)) _formula.weights.Remove(item);
        else _formula.weights[item] = ScoreFormula.DefaultWeight(item);
        RefreshScoring();
    }

    private void Nudge(ScoreFormula.Item item, float by)
    {
        if (!_formula.weights.TryGetValue(item, out float w)) return;
        w = Mathf.Clamp(w + by, -MaxWeight, MaxWeight);
        if (Mathf.Approximately(w, 0f)) w += by; // 0 points would just switch it off
        _formula.weights[item] = w;
        RefreshScoring();
    }

    private void RefreshScoring()
    {
        formulaText.text = ScoreFormula.Describe(ScoreFormula.TermsOf(_formula));
        bool isDefault = _formula.SameAs(ScoreFormula.DefaultSetup());
        formulaTag.text = isDefault ? "Default formula" : "Your formula";
        formulaTag.color = isDefault ? OkText : PinkText;
        SetColor(formulaTag.transform.parent, "", isDefault ? OkBg : PinkBg);

        for (int i = 0; i < scoreRows.Length; i++)
        {
            var row = scoreRows[i];
            bool on = _formula.weights.TryGetValue(ScoreFormula.Choosable[i], out float w);
            row.track.color = on ? Pink : Line;
            row.knob.anchoredPosition = new Vector2(on ? 12f : -12f, 0f);
            row.stepper.SetActive(on);
            if (on)
            {
                row.weight.text = (w > 0 ? "" : "−") + Mathf.Abs(w).ToString("0.#");
                row.weight.color = w < 0 ? Error : Ink;
            }
        }
        bool both = _formula.weights.ContainsKey(ScoreFormula.Item.AvgGame) && _formula.weights.ContainsKey(ScoreFormula.Item.AvgPersonal);
        combineRow.SetActive(both);
        Segment(multiplyButton, _formula.multiply);
        Segment(addButton, !_formula.multiply);
        defaultButton.interactable = !isDefault;
        SetColor(defaultButton.transform, "Label", isDefault ? Faint : Ink);
    }

    private void RefreshFooter()
    {
        int min = _players * 2, joined = Mathf.Min(Joined().Length, _players);
        Check(checks[0], ObjectiveOk, "Game objective");
        Check(checks[1], InstrumentsOk, $"{SelectedCount} / {min} instruments");
        Check(checks[2], PlayersOk, $"{joined} / {_players} players joined");
        backButton.gameObject.SetActive(_tab > 0);
        nextButton.gameObject.SetActive(_tab < tabPages.Length - 1);
        bool ready = ObjectiveOk && InstrumentsOk && PlayersOk;
        Fill(startButton, ready ? Pink : Disabled, ready ? Color.white : Faint);
    }

    // Too early: go to what's missing. Else save the set-up to the room and go to the Role scene.
    private void StartGame()
    {
        if (!IsGm || _starting) return;
        if (!ObjectiveOk) { ShowTab(0); return; }
        if (!InstrumentsOk) { ShowTab(1); return; }
        if (!PlayersOk) return; // the checklist already says who is missing

        _starting = true;
        var selected = _db ? _db.instruments.Where(i => i.isSelected).Select(i => i.id).ToArray() : new int[0];
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
        {
            { BiovalueStatics.GameObjectiveKey, objective.text.Trim() },
            { BiovalueStatics.LinkKey, link.text.Trim() },
            { BiovalueStatics.SelectedInstrumentsKey, selected },
            { BiovalueStatics.LobbyPlayersKey, _players },
            { BiovalueStatics.MaxActionCardsKey, _limit },
            { BiovalueStatics.ScoreFormulaKey, ScoreFormula.Encode(_formula) },
        });
        PhotonNetwork.CurrentRoom.IsOpen = false; // nobody joins a game that has started
        PhotonNetwork.LoadLevel(NextScene);
    }

    // ── 3 · Waiting ──────────────────────────────────────────────────────────

    private void RefreshWaiting()
    {
        if (!PhotonNetwork.InRoom) return;
        var joined = Joined();
        bool known = PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BiovalueStatics.LobbyPlayersKey, out var v) && v is int;
        waitingCount.text = known ? $"· {joined.Length} of {(int)v} players" : $"· {joined.Length} player{(joined.Length == 1 ? "" : "s")}";

        var people = new List<(string name, Color color, bool me)>();
        people.Add(("Game master", Pink, false));
        foreach (var p in joined)
            people.Add((p.IsLocal ? "You · " + NameOf(p) : NameOf(p),
                        PlayerCapsuleHook.ColorFor(TurnRibbon.PlayerNumberOf(p.ActorNumber)), p.IsLocal));
        while (_chips.Count < people.Count) _chips.Add(Instantiate(waitingChipTemplate, waitingList));
        for (int i = 0; i < _chips.Count; i++)
        {
            _chips[i].SetActive(i < people.Count);
            if (i >= people.Count) continue;
            var t = _chips[i].transform;
            SetColor(t, "", i == 0 ? PinkBg : people[i].me ? new Color32(0xE3, 0xEE, 0xFF, 0xFF) : Lilac);
            SetColor(t, "Hex", people[i].color);
            SetText(t, "Name", people[i].name, Ink);
        }
    }

    // ── Looks ────────────────────────────────────────────────────────────────

    private static void SetHint(TMP_Text hint, string text, bool error)
    {
        if (!hint) return;
        hint.text = text;
        hint.color = error ? Error : Muted;
    }

    // Pills: background (root) and label.
    private static void Fill(Button b, Color fill, Color text)
    {
        SetColor(b.transform, "", fill);
        SetColor(b.transform, "Label", text);
    }

    // Choice chips: pink when picked, outlined otherwise (root = border, Fill inside).
    private static void Chip(Button b, bool on)
    {
        SetColor(b.transform, "", on ? Pink : Line);
        SetColor(b.transform, "Fill", on ? Pink : Color.white);
        SetColor(b.transform, "Label", on ? Color.white : Ink);
    }

    // × / + switch.
    private static void Segment(Button b, bool on)
    {
        SetColor(b.transform, "", on ? Color.white : Color.clear);
        SetColor(b.transform, "Label", on ? Ink : Muted);
    }

    private static void Check(GameObject chip, bool ok, string label)
    {
        var t = chip.transform;
        SetColor(t, "", ok ? OkBg : Disabled);
        SetActive(t, "Tick", ok);
        SetActive(t, "Dot", !ok);
        SetText(t, "Label", label, ok ? OkText : Muted);
    }

    private static void SetColor(Transform root, string path, Color color)
    {
        var t = string.IsNullOrEmpty(path) ? root : root.Find(path);
        if (!t) return;
        var g = t.GetComponent<Graphic>();
        if (g) g.color = color;
    }

    private static void SetText(Transform root, string path, string text, Color color)
    {
        var t = root.Find(path)?.GetComponent<TMP_Text>();
        if (!t) return;
        t.text = text;
        t.color = color;
    }

    private static void SetActive(Transform root, string path, bool on)
    {
        var t = root.Find(path);
        if (t) t.gameObject.SetActive(on);
    }
}
