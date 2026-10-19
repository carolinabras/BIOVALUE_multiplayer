using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using static ActionCardBuilder;

// Lobby (mockup "Lobby: create or join, GM set-up, player waiting"), on the Lobby scene's Canvas.
// Same purple and logo as the menu and loading, with fainter hexes. The old panels are switched off
// and CreateJoin is disabled (LobbyScreen does its job now). Run "BioValue/Build Lobby".
public static class LobbyBuilder
{
    private const string ScenePath = "Assets/Scenes/Lobby.unity";
    private const string RootName = "LobbyUI";
    private const string PersonIcon = "Assets/Images/Collaboration/IntroPersonInCircle.png";

    private static readonly Color Pink = new Color32(0xFE, 0x49, 0xA4, 0xFF);
    private static readonly Color Blue = new Color32(0x25, 0x7A, 0xFE, 0xFF);
    private static readonly Color Purple = new Color32(0x54, 0x34, 0xC8, 0xFF);
    private static readonly Color DeepPurple = new Color32(0x46, 0x32, 0x7F, 0xFF);
    private static readonly Color Ink = new Color32(0x24, 0x1B, 0x3A, 0xFF);
    private static readonly Color Muted = new Color32(0x6B, 0x65, 0x84, 0xFF);
    private static readonly Color Line = new Color32(0xDD, 0xD6, 0xEA, 0xFF);
    private static readonly Color Lilac = new Color32(0xF6, 0xF2, 0xFC, 0xFF);
    private static readonly Color Disabled = new Color32(0xF1, 0xEF, 0xF6, 0xFF);
    private static readonly Color Faint = new Color32(0xA9, 0xA3, 0xBC, 0xFF);
    private static readonly Color OkBg = new Color32(0xDD, 0xF3, 0xE0, 0xFF);
    private static readonly Color OkText = new Color32(0x1E, 0x7A, 0x2C, 0xFF);
    private static readonly Color PinkBg = new Color32(0xFF, 0xE3, 0xF1, 0xFF);
    private static readonly Color FooterBg = new Color32(0xFC, 0xFB, 0xFE, 0xFF);
    private static readonly Color StepperBg = new Color32(0xF1, 0xEC, 0xFB, 0xFF);
    private static readonly Color SegmentBg = new Color32(0xED, 0xE8, 0xF6, 0xFF);
    private static readonly Color Body = new Color32(0x4A, 0x44, 0x60, 0xFF);
    private static readonly Color PinkText = new Color32(0x8A, 0x14, 0x50, 0xFF);

    [MenuItem("BioValue/Build Lobby")]
    public static void Build()
    {
        if (!MainMenuBuilder.Ready("Build Lobby") || !MainMenuBuilder.Open(ScenePath, out var scene)) return;

        var canvas = MainMenuBuilder.FindCanvas(scene);
        if (!canvas)
        {
            EditorUtility.DisplayDialog("Build Lobby", "Couldn't find the Canvas in the Lobby scene.", "OK");
            return;
        }
        // LobbyScreen creates/joins rooms and gives the roles now.
        foreach (var old in Object.FindObjectsByType<CreateJoin>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (old.gameObject.scene == scene) { old.enabled = false; EditorUtility.SetDirty(old); }

        var root = MainMenuBuilder.NewRoot(canvas, RootName);
        var s = root.gameObject.AddComponent<LobbyScreen>();
        MainMenuBuilder.Backdrop(root, faint: true);

        // ── Top bar ──
        var logo = MainMenuBuilder.Logo(root);
        Place(logo, 43, 29, 261, 261 * 1596f / 5318f);

        var leave = NewRect("LeaveButton", root);
        leave.anchorMin = leave.anchorMax = leave.pivot = new Vector2(1, 1);
        leave.anchoredPosition = new Vector2(-43, -37);
        leave.sizeDelta = new Vector2(190, 56);
        var leaveButton = MainMenuBuilder.OutlinedButton(leave, "Main menu", 21, 56);
        UnityEventTools.AddPersistentListener(leaveButton.onClick, s.Leave);
        s.leaveLabel = leave.Find("Label").GetComponent<TMP_Text>();

        var pill = MainMenuBuilder.Top(NewRect("RoomPill", root), 37, 470, 60);
        Round(pill, new Color(1, 1, 1, .14f), 30);
        Label(pill, "Room", regular, 20, new Color(1, 1, 1, .75f), 26, 0, 70, 60, TextAlignmentOptions.MidlineLeft);
        s.roomLabel = Label(pill, "Name", bold, 27, Color.white, 92, 0, 240, 60, TextAlignmentOptions.MidlineLeft);
        s.roomLabel.characterSpacing = 4;
        var copy = NewRect("Copy", pill);
        copy.anchorMin = copy.anchorMax = copy.pivot = new Vector2(1, 0.5f);
        copy.anchoredPosition = new Vector2(-10, 0);
        copy.sizeDelta = new Vector2(110, 42);
        var copyButton = PillButton(copy, new Color(1, 1, 1, .22f), "Copy", 19, Color.white);
        UnityEventTools.AddPersistentListener(copyButton.onClick, s.CopyRoomName);
        s.copyLabel = copy.Find("Label").GetComponent<TMP_Text>();
        s.roomPill = pill.gameObject;

        BuildStart(root, s);
        BuildSetup(root, s);
        BuildWaiting(root, s);
        BuildInstrumentDetails(root, s);
        BuildAddDialog(root, s);

        EditorUtility.SetDirty(s);
        MainMenuBuilder.Save(scene, "Lobby built");
    }

    // ── 1 · Create or join ───────────────────────────────────────────────────

    private static void BuildStart(RectTransform root, LobbyScreen s)
    {
        var screen = Stretch(NewRect("StartScreen", root));
        s.startScreen = screen.gameObject;

        Txt(MainMenuBuilder.Top(NewRect("Title", screen), 155, 1400, 66), bold, 53, Color.white, TextAlignmentOptions.Center)
            .text = "Who are you in this game?";
        Txt(MainMenuBuilder.Top(NewRect("Sub", screen), 222, 1400, 38), regular, 27, new Color(1, 1, 1, .75f), TextAlignmentOptions.Center)
            .text = "One person is the game master. Everyone else joins as a player.";

        const float w = 667, h = 600, gap = 48, top = 315;
        // Game master
        var gm = Card(screen, "GameMasterCard", -w - gap / 2, top, w, h);
        CardHead(gm, Pink, "I'm the game master", "Create a room and set up the game.");
        Label(gm, "RoomLabel", bold, 21, Ink, 45, 160, 577, 28).text = "Room name";
        s.createName = Field(gm, "RoomName", 45, 192, 577, 75, "e.g. Lisbon workshop", 27, false);
        s.createHint = Label(gm, "Hint", regular, 20, Muted, 45, 274, 577, 56);
        s.createHint.text = "Your players type this name to join.";
        s.createButton = PillButton(Place(NewRect("CreateButton", gm), 45, h - 45 - 75, 577, 75), Disabled, "Create room", 28, Faint);

        // Player
        var player = Card(screen, "PlayerCard", gap / 2, top, w, h);
        CardHead(player, Blue, "I'm a player", "Join the room your game master made.");
        Label(player, "NameLabel", bold, 21, Ink, 45, 160, 577, 28).text = "Your name";
        s.playerName = Field(player, "YourName", 45, 192, 577, 75, "How the others will see you", 27, false);
        Label(player, "RoomLabel", bold, 21, Ink, 45, 285, 577, 28).text = "Room name";
        s.joinName = Field(player, "RoomName", 45, 317, 577, 75, "Ask your game master", 27, false);
        s.joinHint = Label(player, "Hint", regular, 20, Muted, 45, 399, 577, 56);
        s.joinHint.text = "Capital letters don't matter.";
        s.joinButton = PillButton(Place(NewRect("JoinButton", player), 45, h - 45 - 75, 577, 75), Disabled, "Join room", 28, Faint);
    }

    private static void CardHead(RectTransform card, Color color, string title, string sub)
    {
        var hexRt = Place(NewRect("Icon", card), 45, 40, 69, 80);
        var img = hexRt.gameObject.AddComponent<Image>();
        img.sprite = hex;
        img.color = color;
        img.raycastTarget = false;
        var person = MainMenuBuilder.Centered(NewRect("Person", hexRt), 44);
        var p = person.gameObject.AddComponent<Image>();
        p.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PersonIcon);
        p.preserveAspect = true;
        p.raycastTarget = false;
        if (!p.sprite) Object.DestroyImmediate(person.gameObject);
        Label(card, "Title", bold, 36, Ink, 132, 42, 500, 46).text = title;
        Label(card, "Sub", regular, 23, Muted, 132, 88, 500, 32).text = sub;
    }

    // ── 2 · Game master: set up ──────────────────────────────────────────────

    private static void BuildSetup(RectTransform root, LobbyScreen s)
    {
        var screen = Stretch(NewRect("SetupScreen", root));
        s.setupScreen = screen.gameObject;

        // Players
        var players = Card(screen, "PlayersCard", 0, 0, 440, 915, topLeft: true);
        Place(players, 43, 133, 440, 915);
        Label(players, "Title", bold, 31, Ink, 29, 24, 200, 40).text = "Players";
        s.playersCount = Label(players, "Count", bold, 21, Muted, 180, 30, 231, 32, TextAlignmentOptions.TopRight);
        s.playersHint = Label(players, "Hint", regular, 20, Muted, 29, 70, 382, 30);
        s.playersHint.richText = true;
        var list = Place(NewRect("List", players), 29, 118, 382, 770);
        var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 11;
        layout.childControlHeight = layout.childForceExpandHeight = false;
        layout.childControlWidth = layout.childForceExpandWidth = true;
        s.playerList = list;

        var me = Row(list, "GameMaster", 64);
        Round(me, PinkBg, 19);
        HexIcon(me, "Hex", Pink, 16, 45);
        var meLabel = Label(me, "Name", bold, 24, Ink, 75, 0, 290, 64, TextAlignmentOptions.MidlineLeft);
        meLabel.richText = true;
        meLabel.text = "You <color=#8A1450><font-weight=500>· game master</font-weight></color>";

        var slot = Row(list, "PlayerSlot", 64);
        Round(slot, Color.white, 19);
        Outline(slot, "Border", Line, 19);
        HexIcon(slot, "Hex", Disabled, 16, 45);
        Label(slot, "Name", bold, 24, Faint, 75, 0, 200, 64, TextAlignmentOptions.MidlineLeft).text = "Player";
        Label(slot, "Tag", bold, 19, Faint, 250, 0, 116, 64, TextAlignmentOptions.MidlineRight).text = "Waiting…";
        s.playerSlotTemplate = slot.gameObject;

        // Set-up card
        var card = Card(screen, "SetupCard", 0, 0, 1362, 915, topLeft: true);
        Place(card, 515, 133, 1362, 915);

        string[] tabNames = { "Game", "Instruments", "Action cards", "Scoring" };
        float[] tabWidths = { 170, 250, 262, 200 };
        s.tabButtons = new Button[4];
        float x = 21;
        for (int i = 0; i < 4; i++)
        {
            var tab = Place(NewRect("Tab " + tabNames[i], card), x, 0, tabWidths[i], 75);
            x += tabWidths[i] + 8;
            var hit = tab.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var button = tab.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            MainMenuBuilder.Tints(button);
            var badge = Place(NewRect("Badge", tab), 16, 20, 37, 37);
            Round(badge, Disabled, 18.5f);
            Txt(Stretch(NewRect("Number", badge)), bold, 20, Muted, TextAlignmentOptions.Center).text = (i + 1).ToString();
            var tick = Stretch(NewRect("Tick", badge));
            MainHudBuilder.Tick(tick, OkText, 0.75f);
            tick.gameObject.SetActive(false);
            Label(tab, "Label", bold, 24, Muted, 64, 0, tabWidths[i] - 64, 75, TextAlignmentOptions.MidlineLeft).text = tabNames[i];
            var underline = Place(NewRect("Underline", tab), 8, 71, tabWidths[i] - 16, 4);
            Round(underline, Color.clear, 2);
            s.tabButtons[i] = button;
        }
        Round(Place(NewRect("Divider", card), 0, 75, 1362, 3), Disabled, 1.5f);

        s.tabPages = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            var page = Place(NewRect("Page " + tabNames[i], card), 40, 108, 1282, 690);
            s.tabPages[i] = page.gameObject;
        }
        BuildGamePage((RectTransform)s.tabPages[0].transform, s);
        BuildInstrumentsPage((RectTransform)s.tabPages[1].transform, s);
        BuildCardsPage((RectTransform)s.tabPages[2].transform, s);
        BuildScoringPage((RectTransform)s.tabPages[3].transform, s);

        // Footer
        var footer = Place(NewRect("Footer", card), 0, 819, 1362, 96);
        var footerBg = footer.gameObject.AddComponent<Image>();
        footerBg.color = FooterBg;
        footerBg.raycastTarget = false;
        Round(Place(NewRect("Line", footer), 0, 0, 1362, 3), Disabled, 1.5f);
        var checks = Place(NewRect("Checks", footer), 32, 0, 760, 96);
        var row = checks.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 8;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = false;
        s.checks = new GameObject[3];
        for (int i = 0; i < 3; i++) s.checks[i] = CheckChip(checks, "Check " + (i + 1));

        s.startButton = PillButton(RightOf(footer, "StartButton", 32, 236), Disabled, "Start the game", 24, Faint);
        s.nextButton = PillButton(RightOf(footer, "NextButton", 32 + 236 + 14, 140), Purple, "Next", 24, Color.white);
        s.backButton = ChipButton(RightOf(footer, "BackButton", 32 + 236 + 14 + 140 + 14, 140), "Back", 24, 34.5f);
    }

    private static void BuildGamePage(RectTransform page, LobbyScreen s)
    {
        var title = Label(page, "PlayersTitle", bold, 23, Ink, 0, 0, 900, 32);
        title.richText = true;
        title.text = "Number of players <color=#6B6584><font-weight=500>(not counting you)</font-weight></color>";
        s.countButtons = new Button[LobbyScreen.Counts.Length];
        for (int i = 0; i < s.countButtons.Length; i++)
            s.countButtons[i] = ChipButton(Place(NewRect("Count " + LobbyScreen.Counts[i], page), i * 80, 42, 69, 61),
                                           LobbyScreen.Counts[i].ToString(), 24, 30.5f);

        var objTitle = Label(page, "ObjectiveTitle", bold, 23, Ink, 0, 134, 900, 32);
        objTitle.richText = true;
        objTitle.text = "Game objective <color=#C0263E>*</color>";
        Label(page, "ObjectiveHint", regular, 20, Muted, 0, 166, 1282, 28).text =
            "What should the players achieve together? Everyone sees it during the game.";
        s.objective = Field(page, "Objective", 0, 200, 1282, 150,
                            "e.g. Bring nature back to the river banks of the city centre", 25, true);

        var linkTitle = Label(page, "LinkTitle", bold, 23, Ink, 0, 380, 900, 32);
        linkTitle.richText = true;
        linkTitle.text = "Link to materials <color=#6B6584><font-weight=500>(optional)</font-weight></color>";
        s.link = Field(page, "Link", 0, 418, 1282, 69, "https://…", 24, false);
    }

    private static void BuildInstrumentsPage(RectTransform page, LobbyScreen s)
    {
        Label(page, "Title", bold, 23, Ink, 0, 0, 640, 32).text = "Choose the instruments for this game";
        s.instHint = Label(page, "Hint", regular, 20, Muted, 0, 32, 640, 28);
        s.instHint.enableAutoSizing = true;
        s.instHint.fontSizeMin = 15;
        s.instHint.fontSizeMax = 20;
        s.instHint.textWrappingMode = TextWrappingModes.NoWrap;
        var count = NewRect("Count", page);
        count.anchorMin = count.anchorMax = count.pivot = new Vector2(1, 1);
        count.anchoredPosition = new Vector2(0, -6);
        count.sizeDelta = new Vector2(220, 46);
        Round(count, OkBg, 23);
        s.instCount = Txt(Stretch(NewRect("Label", count)), bold, 22, OkText, TextAlignmentOptions.Center);

        // Cards | List with descriptions
        var views = NewRect("View", page);
        views.anchorMin = views.anchorMax = views.pivot = new Vector2(1, 1);
        views.anchoredPosition = new Vector2(-236, -6);
        views.sizeDelta = new Vector2(390, 46);
        Round(views, Disabled, 23);
        string[] viewNames = { "Cards", "List with descriptions" };
        float[] viewWidths = { 110, 272 };
        s.viewButtons = new Button[2];
        float vx = 4;
        for (int i = 0; i < 2; i++)
        {
            var v = Place(NewRect("View " + viewNames[i], views), vx, 4, viewWidths[i], 38);
            vx += viewWidths[i];
            var vImg = Round(v, Color.clear, 19);
            vImg.raycastTarget = true;
            var vb = v.gameObject.AddComponent<Button>();
            vb.targetGraphic = vImg;
            MainMenuBuilder.Tints(vb);
            Txt(Stretch(NewRect("Label", v)), bold, 19, Muted, TextAlignmentOptions.Center).text = viewNames[i];
            s.viewButtons[i] = vb;
        }

        // Filters: All + the 6 categories
        var filters = Place(NewRect("Filters", page), 0, 78, 1282, 44);
        var row = filters.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 8;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = false;
        s.filterButtons = new Button[1 + LobbyScreen.TypeOrder.Length];
        Color[] dots = { Color.clear, new Color32(0x22, 0x8A, 0xA2, 0xFF), new Color32(0x47, 0xD4, 0x5A, 0xFF),
                         new Color32(0xC0, 0x00, 0x00, 0xFF), new Color32(0xFF, 0xB0, 0x11, 0xFF),
                         new Color32(0x40, 0x40, 0x40, 0xFF), new Color32(0xFF, 0xC0, 0x00, 0xFF) };
        for (int i = 0; i < s.filterButtons.Length; i++)
        {
            string label = i == 0 ? "All" : LobbyScreen.TypeOrder[i - 1].DisplayName();
            s.filterButtons[i] = FlowChip(filters, "Filter " + label, label, i == 0 ? (Color?)null : dots[i]);
        }

        // Grid of tiles, scrolling
        var scroll = Place(NewRect("Scroll", page), 0, 138, 1282, 552);
        var viewport = Stretch(NewRect("Viewport", scroll));
        viewport.gameObject.AddComponent<RectMask2D>();
        var hit = viewport.gameObject.AddComponent<Image>();
        hit.color = Color.clear; // the wheel scrolls between tiles too
        var content = NewRect("Grid", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = Vector2.zero;
        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(414, 98);
        grid.spacing = new Vector2(13, 13);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.padding = new RectOffset(0, 0, 0, 12);
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var sr = scroll.gameObject.AddComponent<ScrollRect>();
        sr.viewport = viewport;
        sr.content = content;
        sr.horizontal = false;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 40;
        s.instGrid = content;
        s.instCardsView = scroll.gameObject;

        // Tile template: category bar, category, name, check box, "i" for the details
        var tile = NewRect("InstrumentTile", content);
        var border = Round(tile, Line, 19);
        border.raycastTarget = true;
        var tileButton = tile.gameObject.AddComponent<Button>();
        tileButton.targetGraphic = border;
        MainMenuBuilder.Tints(tileButton);
        Inset(tile, "Fill", Color.white, 17, 2.5f);
        var bar = Place(NewRect("Bar", tile), 11, 11, 11, 76);
        Round(bar, Line, 5.5f);
        Label(tile, "Type", bold, 17, Muted, 34, 10, 270, 24);
        var name = Label(tile, "Name", bold, 21, Ink, 34, 34, 270, 56);
        name.overflowMode = TextOverflowModes.Ellipsis;
        name.lineSpacing = -12;
        var check = NewRect("Check", tile);
        check.anchorMin = check.anchorMax = check.pivot = new Vector2(1, 0.5f);
        check.anchoredPosition = new Vector2(-62, 0);
        check.sizeDelta = new Vector2(37, 37);
        Round(check, Line, 10);
        Inset(check, "Fill", Color.white, 8, 2.5f);
        var tick = Stretch(NewRect("Tick", check));
        MainHudBuilder.Tick(tick, Color.white, 0.75f);
        var info = NewRect("Info", tile);
        info.anchorMin = info.anchorMax = info.pivot = new Vector2(1, 0.5f);
        info.anchoredPosition = new Vector2(-12, 0);
        info.sizeDelta = new Vector2(42, 42);
        var infoImg = Round(info, Line, 21);
        infoImg.raycastTarget = true;
        Inset(info, "Fill", Color.white, 18.5f, 2.5f);
        Txt(Stretch(NewRect("Label", info)), bold, 25, Purple, TextAlignmentOptions.Center).text = "i";
        var infoButton = info.gameObject.AddComponent<Button>();
        infoButton.targetGraphic = infoImg;
        MainMenuBuilder.Tints(infoButton);
        s.instTileTemplate = tile.gameObject;

        // List with descriptions: every instrument with its categories and full description
        var listScroll = Place(NewRect("ListScroll", page), 0, 138, 1282, 552);
        var listViewport = Stretch(NewRect("Viewport", listScroll));
        listViewport.gameObject.AddComponent<RectMask2D>();
        var listHit = listViewport.gameObject.AddComponent<Image>();
        listHit.color = Color.clear;
        var list = NewRect("List", listViewport);
        list.anchorMin = new Vector2(0, 1);
        list.anchorMax = new Vector2(1, 1);
        list.pivot = new Vector2(0.5f, 1);
        list.sizeDelta = Vector2.zero;
        var listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 11;
        listLayout.padding = new RectOffset(0, 0, 0, 12);
        listLayout.childControlWidth = listLayout.childForceExpandWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandHeight = false;
        list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var listSr = listScroll.gameObject.AddComponent<ScrollRect>();
        listSr.viewport = listViewport;
        listSr.content = list;
        listSr.horizontal = false;
        listSr.movementType = ScrollRect.MovementType.Clamped;
        listSr.scrollSensitivity = 40;
        s.instList = list;
        s.instListView = listScroll.gameObject;
        listScroll.gameObject.SetActive(false);

        var item = NewRect("InstrumentRow", list);
        var rowBorder = Round(item, Line, 19);
        rowBorder.raycastTarget = true;
        var rowButton = item.gameObject.AddComponent<Button>();
        rowButton.targetGraphic = rowBorder;
        MainMenuBuilder.Tints(rowButton);
        Inset(item, "Fill", Color.white, 17, 2.5f);
        var rowLayout = item.gameObject.AddComponent<VerticalLayoutGroup>();
        rowLayout.padding = new RectOffset(100, 28, 14, 16);
        rowLayout.spacing = 2;
        rowLayout.childControlWidth = rowLayout.childForceExpandWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandHeight = false;
        item.gameObject.AddComponent<LayoutElement>().minHeight = 70;
        var rowBar = NewRect("Bar", item);
        rowBar.anchorMin = new Vector2(0, 0);
        rowBar.anchorMax = new Vector2(0, 1);
        rowBar.pivot = new Vector2(0, 0.5f);
        rowBar.offsetMin = new Vector2(14, 14);
        rowBar.offsetMax = new Vector2(25, -14);
        rowBar.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        Round(rowBar, Line, 5.5f);
        var rowCheck = NewRect("Check", item);
        rowCheck.anchorMin = rowCheck.anchorMax = rowCheck.pivot = new Vector2(0, 1);
        rowCheck.anchoredPosition = new Vector2(44, -16);
        rowCheck.sizeDelta = new Vector2(37, 37);
        rowCheck.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        Round(rowCheck, Line, 10);
        Inset(rowCheck, "Fill", Color.white, 8, 2.5f);
        MainHudBuilder.Tick(Stretch(NewRect("Tick", rowCheck)), Color.white, 0.75f);
        var head = Txt(NewRect("Head", item), bold, 23, Ink, TextAlignmentOptions.TopLeft);
        head.richText = true;
        Txt(NewRect("Description", item), regular, 20, Body, TextAlignmentOptions.TopLeft);
        s.instRowTemplate = item.gameObject;

        // + Add a new instrument (stays last)
        var add = NewRect("AddInstrument", content);
        var addBg = Round(add, new Color32(0xFA, 0xF8, 0xFE, 0xFF), 19);
        addBg.raycastTarget = true;
        Outline(add, "Border", new Color32(0xCF, 0xC6, 0xE3, 0xFF), 19);
        var addButton = add.gameObject.AddComponent<Button>();
        addButton.targetGraphic = addBg;
        MainMenuBuilder.Tints(addButton);
        Txt(Stretch(NewRect("Label", add)), bold, 21, Purple, TextAlignmentOptions.Center).text = "+ Add a new instrument";
        s.addInstrumentButton = addButton;
    }

    private static void BuildCardsPage(RectTransform page, LobbyScreen s)
    {
        Label(page, "Title", bold, 23, Ink, 0, 0, 1282, 32).text = "How many action cards can each player play per round?";
        Label(page, "Hint", regular, 20, Muted, 0, 32, 1282, 28).text = "The rules suggest 3.";
        s.limitButtons = new Button[LobbyScreen.Limits.Length];
        float x = 0;
        for (int i = 0; i < s.limitButtons.Length; i++)
        {
            int n = LobbyScreen.Limits[i];
            float w = n == 0 ? 150 : 69;
            s.limitButtons[i] = ChipButton(Place(NewRect("Limit " + (n == 0 ? "None" : n.ToString()), page), x, 80, w, 61),
                                           n == 0 ? "No limit" : n.ToString(), 24, 30.5f);
            x += w + 11;
        }
        var box = Place(NewRect("Explain", page), 0, 175, 1282, 92);
        Round(box, Lilac, 21);
        s.limitText = Label(box, "Text", regular, 24, Ink, 28, 0, 1226, 92, TextAlignmentOptions.MidlineLeft);
    }

    private static void BuildScoringPage(RectTransform page, LobbyScreen s)
    {
        Label(page, "Title", bold, 23, Ink, 0, 0, 900, 32).text = "How the final score is counted";
        Label(page, "Hint", regular, 20, Muted, 0, 32, 1282, 28).text =
            "Switch on what counts and set its points. A minus takes points away. Players see this formula on the results.";

        var box = Place(NewRect("Formula", page), 0, 72, 1282, 124);
        Round(box, Lilac, 21);
        var tag = Place(NewRect("Tag", box), 26, 14, 190, 34);
        Round(tag, OkBg, 17);
        s.formulaTag = Txt(Stretch(NewRect("Label", tag)), bold, 18, OkText, TextAlignmentOptions.Center);
        s.formulaText = Label(box, "Text", bold, 24, Ink, 26, 54, 1230, 64);

        s.scoreRows = new LobbyScreen.ScoreRow[ScoreFormula.Choosable.Length];
        float left = 0, right = 655, colW = 627;
        float y = 216;
        GroupTitle(page, "OBJECTIVES", left, y);
        ScoreRow(page, s, 0, left, y + 30, colW);
        ScoreRow(page, s, 1, left, y + 92, colW);
        // × / + for the two averages
        var combine = Place(NewRect("Combine", page), left, y + 154, colW, 50);
        Label(combine, "Text", medium, 19, Muted, 66, 0, 300, 50, TextAlignmentOptions.MidlineLeft).text = "Combine the two averages";
        var seg = Place(NewRect("Switch", combine), 340, 4, 160, 42);
        Round(seg, SegmentBg, 21);
        s.multiplyButton = SegmentButton(seg, "Multiply", "×", 4);
        s.addButton = SegmentButton(seg, "Add", "+", 82);
        s.combineRow = combine.gameObject;

        GroupTitle(page, "INSTRUMENTS", left, y + 218);
        ScoreRow(page, s, 2, left, y + 248, colW);
        ScoreRow(page, s, 3, left, y + 310, colW);
        ScoreRow(page, s, 4, left, y + 372, colW);

        GroupTitle(page, "ACTION CARDS", right, y);
        ScoreRow(page, s, 5, right, y + 30, colW);
        GroupTitle(page, "COLLABORATION", right, y + 108);
        ScoreRow(page, s, 6, right, y + 138, colW);
        ScoreRow(page, s, 7, right, y + 200, colW);

        s.defaultButton = ChipButton(Place(NewRect("UseDefault", page), right, y + 300, 330, 58), "Use the default formula", 21, 29);
    }

    private static void GroupTitle(RectTransform page, string text, float x, float y)
    {
        var t = Label(page, "Group " + text, bold, 16, Faint, x, y, 400, 24);
        t.text = text;
        t.characterSpacing = 8;
    }

    private static void ScoreRow(RectTransform page, LobbyScreen s, int index, float x, float y, float w)
    {
        var item = ScoreFormula.Choosable[index];
        var row = Place(NewRect("Score " + item, page), x, y, w, 58);
        var v = new LobbyScreen.ScoreRow();

        // switch
        var track = Place(NewRect("Switch", row), 0, 14, 52, 30);
        v.track = Round(track, Line, 15);
        v.track.raycastTarget = true;
        v.toggle = track.gameObject.AddComponent<Button>();
        v.toggle.targetGraphic = v.track;
        MainMenuBuilder.Tints(v.toggle);
        var knob = MainMenuBuilder.Centered(NewRect("Knob", track), 24);
        Round(knob, Color.white, 12);
        v.knob = knob;

        Label(row, "Name", bold, 21, Ink, 66, 2, w - 66 - 150, 30).text = ScoreFormula.Label(item);
        var desc = Label(row, "Description", regular, 16, Muted, 66, 32, w - 66 - 150, 24);
        desc.text = ScoreFormula.Description(item);
        desc.overflowMode = TextOverflowModes.Ellipsis;
        desc.textWrappingMode = TextWrappingModes.NoWrap;

        // − points +
        var stepper = NewRect("Points", row);
        stepper.anchorMin = stepper.anchorMax = stepper.pivot = new Vector2(1, 0.5f);
        stepper.anchoredPosition = Vector2.zero;
        stepper.sizeDelta = new Vector2(140, 40);
        v.minus = RoundButton(stepper, "Minus", "−", 0);
        v.weight = Label(stepper, "Weight", bold, 24, Ink, 40, 0, 60, 40, TextAlignmentOptions.Center);
        v.plus = RoundButton(stepper, "Plus", "+", 100);
        v.stepper = stepper.gameObject;
        s.scoreRows[index] = v;
    }

    private static Button RoundButton(RectTransform parent, string name, string text, float x)
    {
        var rt = Place(NewRect(name, parent), x, 0, 40, 40);
        var img = Round(rt, StepperBg, 20);
        img.raycastTarget = true;
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        MainMenuBuilder.Tints(b);
        Txt(Stretch(NewRect("Label", rt)), bold, 26, DeepPurple, TextAlignmentOptions.Center).text = text;
        return b;
    }

    private static Button SegmentButton(RectTransform parent, string name, string text, float x)
    {
        var rt = Place(NewRect(name, parent), x, 4, 74, 34);
        var img = Round(rt, Color.clear, 17);
        img.raycastTarget = true;
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        MainMenuBuilder.Tints(b);
        Txt(Stretch(NewRect("Label", rt)), bold, 26, Muted, TextAlignmentOptions.Center).text = text;
        return b;
    }

    // ── Instrument details (the "i" on a tile) ───────────────────────────────

    // Left: the piece, big, on its category colour, and its categories. Right: position in the
    // current filter, name, "Selected", the full description, browse arrows and Select / Remove.
    private static void BuildInstrumentDetails(RectTransform root, LobbyScreen s)
    {
        var dialog = Stretch(NewRect("InstrumentDetails", root));
        var dim = dialog.gameObject.AddComponent<Image>();
        dim.color = new Color(0.08f, 0.05f, 0.16f, 0.6f);
        var dimButton = dialog.gameObject.AddComponent<Button>();
        dimButton.transition = Selectable.Transition.None;
        dimButton.targetGraphic = dim;

        var card = NewRect("Card", dialog);
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(1200, 760);
        var cardImg = Round(card, Color.white, 34);
        cardImg.raycastTarget = true; // clicks on the card don't reach the backdrop
        card.gameObject.AddComponent<Mask>(); // the left panel follows the rounded corners
        s.detailsCard = card;

        var left = Place(NewRect("Left", card), 0, 0, 360, 760);
        s.detailsLeft = left.gameObject.AddComponent<Image>();
        s.detailsLeft.raycastTarget = false;
        var piece = Place(NewRect("Piece", left), 80, 160, 200, 230);
        s.detailsRing = piece.gameObject.AddComponent<Image>();
        s.detailsRing.sprite = hex;
        s.detailsRing.raycastTarget = false;
        var inner = Stretch(NewRect("Inner", piece));
        inner.offsetMin = new Vector2(13, 15);
        inner.offsetMax = new Vector2(-13, -15);
        var innerImg = inner.gameObject.AddComponent<Image>();
        innerImg.sprite = hex;
        innerImg.raycastTarget = false;
        s.detailsNumber = Txt(Stretch(NewRect("Number", piece)), bold, 60, Ink, TextAlignmentOptions.Center);
        s.detailsCategories = new GameObject[2];
        for (int i = 0; i < 2; i++)
        {
            var chip = Place(NewRect("Category " + (i + 1), left), 30, 425 + i * 58, 300, 46);
            Round(chip, Color.white, 23);
            Round(Place(NewRect("Dot", chip), 20, 16.5f, 13, 13), Line, 6.5f);
            var label = Label(chip, "Label", bold, 19, Ink, 42, 0, 240, 46, TextAlignmentOptions.MidlineLeft);
            label.enableAutoSizing = true;
            label.fontSizeMin = 14;
            label.fontSizeMax = 19;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            s.detailsCategories[i] = chip.gameObject;
        }

        const float rx = 405, rw = 750;
        s.detailsPosition = Label(card, "Eyebrow", bold, 19, Muted, rx, 36, 640, 30);
        s.detailsPosition.characterSpacing = 10;
        var close = NewRect("Close", card);
        close.anchorMin = close.anchorMax = close.pivot = new Vector2(1, 1);
        close.anchoredPosition = new Vector2(-36, -28);
        close.sizeDelta = new Vector2(54, 54);
        var closeImg = Round(close, Disabled, 27);
        closeImg.raycastTarget = true;
        var closeButton = close.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImg;
        MainMenuBuilder.Tints(closeButton);
        MainHudBuilder.Cross(close, Ink, 0.9f);
        s.detailsClose = new[] { dimButton, closeButton };

        s.detailsName = Label(card, "Name", bold, 42, Ink, rx, 72, rw, 112);
        s.detailsName.enableAutoSizing = true;
        s.detailsName.fontSizeMin = 28;
        s.detailsName.fontSizeMax = 42;
        s.detailsName.lineSpacing = -14;

        var badge = Place(NewRect("Selected", card), rx, 190, 300, 38);
        Round(badge, PinkBg, 19);
        MainHudBuilder.Tick(Place(NewRect("Tick", badge), 12, 4, 30, 30), PinkText, 0.6f);
        Label(badge, "Label", bold, 19, PinkText, 44, 0, 250, 38, TextAlignmentOptions.MidlineLeft).text = "Selected for this game";
        s.detailsSelected = badge.gameObject;

        var scroll = Place(NewRect("Description", card), rx, 244, rw, 380);
        var viewport = Stretch(NewRect("Viewport", scroll));
        viewport.gameObject.AddComponent<RectMask2D>();
        var hit = viewport.gameObject.AddComponent<Image>();
        hit.color = Color.clear;
        var text = NewRect("Text", viewport);
        text.anchorMin = new Vector2(0, 1);
        text.anchorMax = new Vector2(1, 1);
        text.pivot = new Vector2(0.5f, 1);
        text.sizeDelta = Vector2.zero;
        s.detailsDescription = Txt(text, regular, 27, Body, TextAlignmentOptions.TopLeft);
        text.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var sr = scroll.gameObject.AddComponent<ScrollRect>();
        sr.viewport = viewport;
        sr.content = text;
        sr.horizontal = false;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 40;
        s.detailsScroll = sr;

        s.detailsPrev = ArrowButton(card, "Previous", rx, 652, -1);
        s.detailsNext = ArrowButton(card, "Next", rx + 82, 652, 1);
        s.detailsSelect = PillButton(Place(NewRect("Select", card), 1200 - 45 - 380, 652, 380, 68), Pink, "Select for this game", 24, Color.white);

        s.detailsDialog = dialog.gameObject;
        dialog.gameObject.SetActive(false);
    }

    // Round outlined button with a chevron pointing left (-1) or right (1).
    private static Button ArrowButton(RectTransform parent, string name, float x, float y, int dir)
    {
        var rt = Place(NewRect(name, parent), x, y, 68, 68);
        var img = Round(rt, Line, 34);
        img.raycastTarget = true;
        Inset(rt, "Fill", Color.white, 31.5f, 2.5f);
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        MainMenuBuilder.Tints(b);
        var icon = Stretch(NewRect("Chevron", rt));
        MainHudBuilder.Bar(icon, Ink, new Vector2(0, 6), new Vector2(18, 5), dir < 0 ? 42f : -42f);
        MainHudBuilder.Bar(icon, Ink, new Vector2(0, -6), new Vector2(18, 5), dir < 0 ? -42f : 42f);
        return b;
    }

    // ── New instrument dialog ────────────────────────────────────────────────

    private static void BuildAddDialog(RectTransform root, LobbyScreen s)
    {
        var dialog = Stretch(NewRect("AddInstrumentDialog", root));
        var dim = dialog.gameObject.AddComponent<Image>();
        dim.color = new Color(0.08f, 0.05f, 0.16f, 0.6f);
        dim.raycastTarget = true;
        var card = NewRect("Card", dialog);
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(760, 640);
        Round(card, Color.white, 32);

        Label(card, "Title", bold, 36, Ink, 45, 36, 670, 46).text = "New instrument";
        Label(card, "NameLabel", bold, 21, Ink, 45, 100, 670, 28).text = "Name";
        s.newName = Field(card, "Name", 45, 132, 670, 69, "e.g. Green roofs subsidy", 25, false);
        Label(card, "DescriptionLabel", bold, 21, Ink, 45, 220, 670, 28).text = "Description (optional)";
        s.newDescription = Field(card, "Description", 45, 252, 670, 110, "What it does, in one or two sentences", 22, true);
        Label(card, "TypeLabel", bold, 21, Ink, 45, 380, 670, 28).text = "Category";
        s.newTypeButtons = new Button[LobbyScreen.TypeOrder.Length];
        for (int i = 0; i < s.newTypeButtons.Length; i++)
        {
            float x = 45 + (i % 3) * 228, y = 414 + (i / 3) * 58;
            string label = LobbyScreen.TypeOrder[i] == InstrumentType.Planning ? "Planning" : LobbyScreen.TypeOrder[i].DisplayName();
            s.newTypeButtons[i] = ChipButton(Place(NewRect("Type " + label, card), x, y, 214, 48), label, 20, 24);
        }
        var cancel = ChipButton(Place(NewRect("Cancel", card), 45, 552, 200, 62), "Cancel", 22, 31);
        UnityEventTools.AddPersistentListener(cancel.onClick, s.CloseAddDialog);
        s.addConfirm = PillButton(Place(NewRect("Add", card), 265, 552, 450, 62), Disabled, "Add instrument", 24, Faint);
        s.addDialog = dialog.gameObject;
        dialog.SetAsLastSibling();
        dialog.gameObject.SetActive(false);
    }

    // ── 3 · Player: waiting ──────────────────────────────────────────────────

    private static void BuildWaiting(RectTransform root, LobbyScreen s)
    {
        var screen = Stretch(NewRect("WaitingScreen", root));
        s.waitingScreen = screen.gameObject;
        var card = Card(screen, "WaitingCard", -853 / 2f, 173, 853, 690);

        Color[] colors = { Pink, Blue, new Color32(0xFD, 0xAA, 0x12, 0xFF) };
        for (int i = 0; i < 3; i++)
        {
            var cell = NewRect("Cell", card);
            cell.anchorMin = cell.anchorMax = new Vector2(0.5f, 1f);
            cell.pivot = new Vector2(0.5f, 0.5f);
            cell.anchoredPosition = new Vector2((i - 1) * 88, -86);
            cell.sizeDelta = new Vector2(72, 83);
            var img = cell.gameObject.AddComponent<Image>();
            img.sprite = hex;
            img.color = colors[i];
            img.raycastTarget = false;
            cell.gameObject.AddComponent<HexPop>().delay = i * 0.2f;
        }
        s.waitingTitle = Txt(MainMenuBuilder.Top(NewRect("Title", card), 150, 760, 54), bold, 43, Ink, TextAlignmentOptions.Center);
        s.waitingTitle.text = "You're in!";
        s.waitingSub = Txt(MainMenuBuilder.Top(NewRect("Sub", card), 206, 760, 34), regular, 25, Muted, TextAlignmentOptions.Center);
        s.waitingSub.text = "Waiting for the game master to start the game.";

        Label(card, "InTheRoom", bold, 21, Ink, 53, 275, 150, 30).text = "In the room";
        s.waitingCount = Label(card, "Count", medium, 21, Muted, 186, 275, 400, 30);

        var list = Place(NewRect("List", card), 53, 318, 747, 340);
        var grid = list.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(368, 52);
        grid.spacing = new Vector2(11, 11);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        s.waitingList = list;
        var chip = NewRect("Person", list);
        Round(chip, Lilac, 26);
        HexIcon(chip, "Hex", Pink, 12, 29);
        Label(chip, "Name", bold, 21, Ink, 52, 0, 300, 52, TextAlignmentOptions.MidlineLeft);
        s.waitingChipTemplate = chip.gameObject;
    }

    // ── Pieces ───────────────────────────────────────────────────────────────

    // White card; `x` is from the screen centre (or from the left with topLeft), `y` from the top.
    internal static RectTransform Card(RectTransform parent, string name, float x, float y, float w, float h, bool topLeft = false)
    {
        var card = NewRect(name, parent);
        if (topLeft) Place(card, x, y, w, h);
        else
        {
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 1f);
            card.pivot = new Vector2(0f, 1f);
            card.anchoredPosition = new Vector2(x, -y);
            card.sizeDelta = new Vector2(w, h);
        }
        Round(card, Color.white, 32);
        var shadow = card.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.04f, 0.02f, 0.16f, 0.25f);
        shadow.effectDistance = new Vector2(0, -8);
        return card;
    }

    internal static TextMeshProUGUI Label(RectTransform parent, string name, TMP_FontAsset font, float size, Color color,
                                         float x, float y, float w, float h,
                                         TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
    {
        var t = Txt(Place(NewRect(name, parent), x, y, w, h), font, size, color, align);
        return t;
    }

    private static RectTransform Row(RectTransform list, string name, float height)
    {
        var row = NewRect(name, list);
        var le = row.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = le.minHeight = height;
        return row;
    }

    private static void HexIcon(RectTransform parent, string name, Color color, float x, float w)
    {
        var rt = NewRect(name, parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0);
        rt.sizeDelta = new Vector2(w, w * 1.15f);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = hex;
        img.color = color;
        img.raycastTarget = false;
    }

    // Rounded outline on top of the parent (same corners).
    private static void Outline(RectTransform parent, string name, Color color, float radius)
    {
        var img = Stretch(NewRect(name, parent)).gameObject.AddComponent<Image>();
        img.sprite = MainMenuBuilder.RoundRing();
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 24f / radius;
        img.color = color;
        img.raycastTarget = false;
    }

    // A rounded fill `inset` inside the parent (the parent's colour shows as a border).
    internal static Image Inset(RectTransform parent, string name, Color color, float radius, float inset)
    {
        var rt = Stretch(NewRect(name, parent));
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
        return Round(rt, color, radius);
    }

    private static RectTransform RightOf(RectTransform footer, string name, float right, float w)
    {
        var rt = NewRect(name, footer);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0.5f);
        rt.anchoredPosition = new Vector2(-right, 0);
        rt.sizeDelta = new Vector2(w, 69);
        return rt;
    }

    // Filled pill: the root is the fill, "Label" the text.
    internal static Button PillButton(RectTransform rt, Color fill, string text, float size, Color textColor)
    {
        var img = Round(rt, fill, rt.sizeDelta.y / 2);
        img.raycastTarget = true;
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        MainMenuBuilder.Tints(b);
        Txt(Stretch(NewRect("Label", rt)), bold, size, textColor, TextAlignmentOptions.Center).text = text;
        return b;
    }

    // Outlined chip: the root is the border, "Fill" inside, "Label" on top.
    internal static Button ChipButton(RectTransform rt, string text, float size, float radius)
    {
        var img = Round(rt, Line, radius);
        img.raycastTarget = true;
        Inset(rt, "Fill", Color.white, radius - 2.5f, 2.5f);
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        MainMenuBuilder.Tints(b);
        Txt(Stretch(NewRect("Label", rt)), bold, size, Ink, TextAlignmentOptions.Center).text = text;
        return b;
    }

    // Chip that sizes to its label (filters): border, Fill, an optional colour dot, Label.
    private static Button FlowChip(RectTransform row, string name, string text, Color? dot)
    {
        var rt = NewRect(name, row);
        var img = Round(rt, Line, 20);
        img.raycastTarget = true;
        var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 6, 6);
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        var fitter = rt.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        Inset(rt, "Fill", Color.white, 17.5f, 2.5f);
        if (dot.HasValue)
        {
            var d = NewRect("Dot", rt);
            var le = d.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = le.minWidth = le.minHeight = 13;
            Round(d, dot.Value, 6.5f);
        }
        var label = Txt(NewRect("Label", rt), bold, 19, Ink, TextAlignmentOptions.Center);
        label.text = text;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        MainMenuBuilder.Tints(b);
        var size = rt.gameObject.AddComponent<LayoutElement>();
        size.preferredHeight = size.minHeight = 44;
        return b;
    }

    // Footer checklist item: green with a tick when done, grey with a dot when not.
    private static GameObject CheckChip(RectTransform row, string name)
    {
        var rt = NewRect(name, row);
        Round(rt, Disabled, 18);
        var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 16, 6, 6);
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        rt.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        var size = rt.gameObject.AddComponent<LayoutElement>();
        size.preferredHeight = size.minHeight = 36;

        var tick = NewRect("Tick", rt);
        var tle = tick.gameObject.AddComponent<LayoutElement>();
        tle.preferredWidth = tle.preferredHeight = tle.minWidth = tle.minHeight = 20;
        MainHudBuilder.Tick(tick, OkText, 0.6f);
        var dot = NewRect("Dot", rt);
        var dle = dot.gameObject.AddComponent<LayoutElement>();
        dle.preferredWidth = dle.preferredHeight = dle.minWidth = dle.minHeight = 10;
        Round(dot, Faint, 5);
        var label = Txt(NewRect("Label", rt), bold, 19, Muted, TextAlignmentOptions.MidlineLeft);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.text = name;
        return rt.gameObject;
    }

    // Text field: light border, white inside.
    internal static TMP_InputField Field(RectTransform parent, string name, float x, float y, float w, float h,
                                        string placeholder, float size, bool multiline)
    {
        var rt = Place(NewRect(name, parent), x, y, w, h);
        var border = Round(rt, Line, 18);
        border.raycastTarget = true;
        Inset(rt, "Fill", Color.white, 15.5f, 2.5f);
        var area = Stretch(NewRect("Text Area", rt));
        area.offsetMin = new Vector2(20, multiline ? 14 : 0);
        area.offsetMax = new Vector2(-20, multiline ? -14 : 0);
        area.gameObject.AddComponent<RectMask2D>();
        var align = multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
        var ph = Txt(Stretch(NewRect("Placeholder", area)), regular, size, Faint, align);
        ph.text = placeholder;
        ph.textWrappingMode = multiline ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        var text = Txt(Stretch(NewRect("Text", area)), medium, size, Ink, align);
        text.textWrappingMode = multiline ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        text.text = "";

        var input = rt.gameObject.AddComponent<TMP_InputField>();
        input.textViewport = area;
        input.textComponent = text;
        input.placeholder = ph;
        input.targetGraphic = border;
        input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
        input.characterLimit = multiline ? 400 : 60;
        input.richText = false;
        input.fontAsset = medium;
        input.pointSize = size;
        input.caretColor = Ink;
        input.customCaretColor = true;
        input.selectionColor = new Color(1f, 0.29f, 0.64f, 0.25f);
        return input;
    }
}
