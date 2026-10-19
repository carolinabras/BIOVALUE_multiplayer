using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static ActionCardBuilder;

// Role scene (mockup "Role": player form + live card, GM overview), on the Role scene's Canvas.
// Same purple, faint hexes and logo as the Lobby. The old panels are switched off and the old
// Role / GM_StatusRoleScreen scripts disabled (RoleScreen does their job now). Run "BioValue/Build Role".
public static class RoleBuilder
{
    private const string ScenePath = "Assets/Scenes/Role.unity";
    private const string RootName = "RoleUI";

    private static readonly Color Pink = new Color32(0xFE, 0x49, 0xA4, 0xFF);
    private static readonly Color Purple = new Color32(0x54, 0x34, 0xC8, 0xFF);
    private static readonly Color Eyebrow = new Color32(0x6A, 0x55, 0xA8, 0xFF);
    private static readonly Color Ink = new Color32(0x24, 0x1B, 0x3A, 0xFF);
    private static readonly Color Muted = new Color32(0x6B, 0x65, 0x84, 0xFF);
    private static readonly Color Faint = new Color32(0xA9, 0xA3, 0xBC, 0xFF);
    private static readonly Color Disabled = new Color32(0xF1, 0xEF, 0xF6, 0xFF);
    private static readonly Color NumberBg = new Color32(0xF1, 0xEC, 0xFB, 0xFF);
    private static readonly Color Green = new Color32(0x47, 0xB0, 0x4B, 0xFF);
    private static readonly Color OkText = new Color32(0x1E, 0x7A, 0x2C, 0xFF);
    private static readonly Color Glass = new Color(1f, 1f, 1f, 0.14f);
    private static readonly Color Blue = new Color32(0x2F, 0x6F, 0xB0, 0xFF);

    [MenuItem("BioValue/Build Role")]
    public static void Build()
    {
        if (!MainMenuBuilder.Ready("Build Role") || !MainMenuBuilder.Open(ScenePath, out var scene)) return;

        var canvas = MainMenuBuilder.FindCanvas(scene);
        if (!canvas)
        {
            EditorUtility.DisplayDialog("Build Role", "Couldn't find the Canvas in the Role scene.", "OK");
            return;
        }
        // RoleScreen saves the role, shows the GM overview and starts the game now.
        foreach (var old in Object.FindObjectsByType<Role>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (old.gameObject.scene == scene) { old.enabled = false; EditorUtility.SetDirty(old); }
        foreach (var old in Object.FindObjectsByType<GM_StatusRoleScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (old.gameObject.scene == scene) { old.enabled = false; EditorUtility.SetDirty(old); }

        var root = MainMenuBuilder.NewRoot(canvas, RootName);
        var s = root.gameObject.AddComponent<RoleScreen>();
        MainMenuBuilder.Backdrop(root, faint: true);

        BuildTopBar(root, s);
        BuildPlayer(root, s);
        BuildGm(root, s);

        EditorUtility.SetDirty(s);
        MainMenuBuilder.Save(scene, "Role built");
    }

    // ── Top bar: logo, the game objective (and materials), the room ─────────

    private static void BuildTopBar(RectTransform root, RoleScreen s)
    {
        var logo = MainMenuBuilder.Logo(root);
        Place(logo, 43, 29, 261, 261 * 1596f / 5318f);

        const float w = 1093, h = 60;
        var pill = MainMenuBuilder.Top(NewRect("ObjectivePill", root), 37, w, h);
        Round(pill, Glass, h / 2);
        var eyebrow = LobbyBuilder.Label(pill, "Eyebrow", bold, 17, new Color(1, 1, 1, .75f), 27, 0, 190, h, TextAlignmentOptions.MidlineLeft);
        eyebrow.text = "GAME OBJECTIVE";
        eyebrow.characterSpacing = 6;
        s.gameObjective = LobbyBuilder.Label(pill, "Objective", bold, 24, Color.white, 215, 0, w - 215 - 175, h, TextAlignmentOptions.MidlineLeft);
        s.gameObjective.textWrappingMode = TextWrappingModes.NoWrap;
        s.gameObjective.overflowMode = TextOverflowModes.Ellipsis;
        s.gameObjective.text = "Bring nature back to the river banks";
        var materials = NewRect("Materials", pill);
        materials.anchorMin = materials.anchorMax = materials.pivot = new Vector2(1, 0.5f);
        materials.anchoredPosition = new Vector2(-10, 0);
        materials.sizeDelta = new Vector2(150, 42);
        s.materialsButton = LobbyBuilder.PillButton(materials, new Color(1, 1, 1, .22f), "Materials", 19, Color.white);

        var room = NewRect("Room", root);
        room.anchorMin = room.anchorMax = room.pivot = new Vector2(1, 1);
        room.anchoredPosition = new Vector2(-43, -37);
        room.sizeDelta = new Vector2(261, h);
        s.roomLabel = Txt(room, regular, 20, new Color(1, 1, 1, .7f), TextAlignmentOptions.MidlineRight);
        s.roomLabel.richText = true;
        s.roomLabel.text = "Room";
    }

    // ── Player: the form and the live card ──────────────────────────────────

    private static void BuildPlayer(RectTransform root, RoleScreen s)
    {
        var screen = Stretch(NewRect("PlayerScreen", root));
        s.playerScreen = screen.gameObject;

        s.playerTitle = Txt(MainMenuBuilder.Top(NewRect("Title", screen), 136, 1400, 64), bold, 51, Color.white, TextAlignmentOptions.Center);
        s.playerTitle.text = "Who are you playing?";
        s.playerSub = Txt(MainMenuBuilder.Top(NewRect("Sub", screen), 202, 1500, 36), regular, 25, new Color(1, 1, 1, .75f), TextAlignmentOptions.Center);
        s.playerSub.text = "Everyone represents an entity with its own goal.";

        // Form
        const float fw = 933, inX = 92, inW = fw - inX - 45;
        var form = LobbyBuilder.Card(screen, "Form", -803, 275, fw, 713);
        FieldHead(form, 1, "Your name", "Filled in from the lobby. Change it if you like.", 37);
        s.nameInput = LobbyBuilder.Field(form, "Name", inX, 107, inW, 69, "e.g. James", 25, false);
        FieldHead(form, 2, "The entity you represent", "A council, a company, an association, a group of residents…", 200);
        s.entityInput = LobbyBuilder.Field(form, "Entity", inX, 270, inW, 69, "e.g. Game Dev Association", 25, false);
        s.entityInput.characterLimit = 60;
        FieldHead(form, 3, "Your personal objective", "What does your entity want from this game? One or two sentences.", 363);
        s.objectiveInput = LobbyBuilder.Field(form, "Objective", inX, 433, inW, 139, "e.g. Help game studios in Portugal grow and work together", 25, true);
        s.objectiveInput.characterLimit = RoleScreen.ObjectiveLimit;
        s.objectiveInput.lineType = TMP_InputField.LineType.MultiLineSubmit; // Enter leaves the field
        s.objectiveCount = LobbyBuilder.Label(form, "Count", medium, 19, Faint, fw - 45 - 200, 363, 200, 32, TextAlignmentOptions.TopRight);
        s.objectiveCount.text = "0 / " + RoleScreen.ObjectiveLimit;

        const float fy = 598, readyW = 250, editW = 140;
        s.readyHint = LobbyBuilder.Label(form, "Hint", regular, 20, Muted, 45, fy, fw - 45 - readyW - 16 - editW - 16 - 45, 75, TextAlignmentOptions.MidlineLeft);
        s.readyHint.textWrappingMode = TextWrappingModes.Normal;
        s.readyHint.text = "Fill in all three to get ready.";
        s.editButton = LobbyBuilder.ChipButton(Place(NewRect("EditButton", form), fw - 45 - readyW - 16 - editW, fy, editW, 75), "Edit", 25, 37.5f);
        s.readyButton = LobbyBuilder.PillButton(Place(NewRect("ReadyButton", form), fw - 45 - readyW, fy, readyW, 75), Disabled, "I'm ready", 28, Faint);

        // Live card
        const float px = 184, pw = 619;
        var head = At(screen, "PreviewEyebrow", px, 275, pw, 30);
        var headText = Txt(head, bold, 20, new Color(1, 1, 1, .8f), TextAlignmentOptions.TopLeft);
        headText.text = "HOW THE OTHERS WILL SEE YOU";
        headText.characterSpacing = 6;

        var card = LobbyBuilder.Card(screen, "Preview", px, 316, pw, 410);
        var band = Place(NewRect("Band", card), 0, 0, pw, 120);
        s.previewBand = Round(band, Blue, 32);
        var square = Place(NewRect("Square", band), 0, 60, pw, 60);
        square.gameObject.AddComponent<Image>().color = Blue;
        square.GetComponent<Image>().raycastTarget = false;
        var circle = Place(NewRect("Circle", band), 29, 24, 72, 72);
        Round(circle, new Color(1, 1, 1, .25f), 36);
        s.previewNumber = Txt(Stretch(NewRect("Number", circle)), bold, 35, Color.white, TextAlignmentOptions.Center);
        s.previewNumber.text = "2";
        s.previewName = LobbyBuilder.Label(band, "Name", bold, 35, Color.white, 122, 18, pw - 122 - 24, 48, TextAlignmentOptions.MidlineLeft);
        s.previewName.textWrappingMode = TextWrappingModes.NoWrap;
        s.previewName.overflowMode = TextOverflowModes.Ellipsis;
        s.previewEntity = LobbyBuilder.Label(band, "Entity", medium, 23, new Color(1, 1, 1, .9f), 122, 66, pw - 122 - 24, 32, TextAlignmentOptions.MidlineLeft);
        s.previewEntity.textWrappingMode = TextWrappingModes.NoWrap;
        s.previewEntity.overflowMode = TextOverflowModes.Ellipsis;

        var eyebrow = LobbyBuilder.Label(card, "Eyebrow", bold, 17, Eyebrow, 29, 144, pw - 58, 24);
        eyebrow.text = "PERSONAL OBJECTIVE";
        eyebrow.characterSpacing = 6;
        s.previewObjective = LobbyBuilder.Label(card, "Objective", medium, 25, Ink, 29, 172, pw - 58, 150);
        s.previewObjective.textWrappingMode = TextWrappingModes.Normal;
        s.previewObjective.overflowMode = TextOverflowModes.Ellipsis;
        var note = LobbyBuilder.Label(card, "Private", regular, 18, Muted, 29, 334, pw - 58, 56, TextAlignmentOptions.BottomLeft);
        note.textWrappingMode = TextWrappingModes.Normal;
        note.text = "Only you and the game master see your objective. At the end, you choose whether everyone sees it.";

        // "You're ready"
        var banner = At(screen, "ReadyBanner", px, 748, pw, 90);
        Round(banner, Glass, 24);
        var tickCircle = Place(NewRect("Circle", banner), 24, 22, 46, 46);
        Round(tickCircle, Green, 23);
        MainHudBuilder.Tick(tickCircle, Color.white, 0.8f);
        LobbyBuilder.Label(banner, "Title", bold, 24, Color.white, 88, 14, pw - 110, 34).text = "You're ready";
        s.othersReady = LobbyBuilder.Label(banner, "Others", regular, 20, new Color(1, 1, 1, .8f), 88, 46, pw - 110, 30);
        s.readyBanner = banner.gameObject;
        banner.gameObject.SetActive(false);
    }

    private static void FieldHead(RectTransform form, int n, string label, string hint, float y)
    {
        var number = Place(NewRect("Number" + n, form), 45, y, 35, 35);
        Round(number, NumberBg, 17.5f);
        Txt(Stretch(NewRect("Label", number)), bold, 20, Purple, TextAlignmentOptions.Center).text = n.ToString();
        LobbyBuilder.Label(form, "Label" + n, bold, 24, Ink, 92, y - 1, 560, 36, TextAlignmentOptions.MidlineLeft).text = label;
        var h = LobbyBuilder.Label(form, "Hint" + n, regular, 20, Muted, 92, y + 36, 796, 30);
        h.textWrappingMode = TextWrappingModes.NoWrap;
        h.overflowMode = TextOverflowModes.Ellipsis;
        h.text = hint;
    }

    // ── Game master: everyone's role, live ───────────────────────────────────

    private static void BuildGm(RectTransform root, RoleScreen s)
    {
        var screen = Stretch(NewRect("GmScreen", root));
        s.gmScreen = screen.gameObject;

        Txt(At(screen, "Title", -880, 130, 1400, 64), bold, 51, Color.white, TextAlignmentOptions.TopLeft)
            .text = "The players are writing their roles";
        Txt(At(screen, "Sub", -880, 196, 1400, 36), regular, 25, new Color(1, 1, 1, .75f), TextAlignmentOptions.TopLeft)
            .text = "Read who everyone represents. Start the game when everyone is ready.";

        var count = At(screen, "ReadyCount", 960 - 80 - 250, 172, 250, 56);
        s.readyCountPill = Round(count, Glass, 28);
        s.readyCount = Txt(Stretch(NewRect("Label", count)), bold, 27, Color.white, TextAlignmentOptions.Center);
        s.readyCount.text = "0 of 0 ready";

        // Two columns of player cards; scrolls when there are more than six players.
        const float listW = 1760, cardW = (listW - 21) / 2, cardH = 193;
        var view = At(screen, "List", -880, 262, listW, 680);
        view.gameObject.AddComponent<RectMask2D>().padding = new Vector4(0, 0, 0, 0);
        var hit = view.gameObject.AddComponent<Image>();
        hit.color = Color.clear;
        var content = NewRect("Content", view);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(cardW, cardH);
        grid.spacing = new Vector2(21, 21);
        grid.padding = new RectOffset(0, 0, 13, 13); // room for the card shadows
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = view;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40;
        s.cardList = content;
        s.cardTemplate = PlayerCard(content, cardW, cardH).gameObject;

        s.gmHint = Txt(At(screen, "Hint", -880, 965, 1100, 80), regular, 23, new Color(1, 1, 1, .8f), TextAlignmentOptions.MidlineLeft);
        s.gmHint.text = "Waiting for the players.";
        const float bw = 360;
        s.startButton = LobbyBuilder.PillButton(At(screen, "StartButton", 960 - 80 - bw, 965, bw, 80), Pink, "Start the game", 30, Color.white);
        s.startAnywayButton = MainMenuBuilder.OutlinedButton(At(screen, "StartAnywayButton", 960 - 80 - bw, 965, bw, 80), "Start anyway", 30, 80);
        s.startButton.gameObject.SetActive(false);
    }

    // The template RoleScreen copies per player (it fills the colour, number, texts and status).
    private static RectTransform PlayerCard(RectTransform list, float w, float h)
    {
        var card = NewRect("PlayerCard", list);
        Round(card, Color.white, 29);
        var shadow = card.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.04f, 0.02f, 0.16f, 0.25f);
        shadow.effectDistance = new Vector2(0, -6);

        const float side = 77, x = side + 24, statusW = 200;
        var strip = Place(NewRect("Side", card), 0, 0, side, h);
        Round(strip, PlayerCapsuleHook.ColorFor(1), 29);
        var square = Place(NewRect("Square", strip), side / 2, 0, side / 2, h);
        var sq = square.gameObject.AddComponent<Image>();
        sq.color = PlayerCapsuleHook.ColorFor(1);
        sq.raycastTarget = false;
        LobbyBuilder.Label(strip, "Number", bold, 35, Color.white, 0, 18, side, 46, TextAlignmentOptions.Center).text = "1";

        var name = LobbyBuilder.Label(card, "Name", bold, 29, Ink, x, 18, w - x - 24 - statusW - 12, 38, TextAlignmentOptions.MidlineLeft);
        name.textWrappingMode = TextWrappingModes.NoWrap;
        name.overflowMode = TextOverflowModes.Ellipsis;
        name.text = "Player";
        var entity = LobbyBuilder.Label(card, "Entity", medium, 21, Muted, x, 56, w - x - 24 - statusW - 12, 30, TextAlignmentOptions.MidlineLeft);
        entity.textWrappingMode = TextWrappingModes.NoWrap;
        entity.overflowMode = TextOverflowModes.Ellipsis;
        entity.text = "No entity yet";

        var status = Place(NewRect("Status", card), w - 24 - statusW, 24, statusW, 44);
        Round(status, Disabled, 22);
        var tick = Place(NewRect("Tick", status), 26, 12, 20, 20); // RoleScreen shows it when ready
        MainHudBuilder.Tick(tick, OkText, 0.65f);
        tick.gameObject.SetActive(false);
        var label = Txt(Stretch(NewRect("Label", status)), bold, 20, Muted, TextAlignmentOptions.Center);
        label.text = "Not started";

        var eyebrow = LobbyBuilder.Label(card, "Eyebrow", bold, 16, Eyebrow, x, 98, w - x - 24, 22);
        eyebrow.text = "PERSONAL OBJECTIVE";
        eyebrow.characterSpacing = 6;
        var objective = LobbyBuilder.Label(card, "Objective", medium, 23, Ink, x, 122, w - x - 24, 60);
        objective.textWrappingMode = TextWrappingModes.Normal;
        objective.overflowMode = TextOverflowModes.Ellipsis;
        objective.text = "Still writing their objective…";
        return card;
    }

    // From the screen's top centre: `x` to the left edge (negative = left of centre), `y` down from the top.
    private static RectTransform At(RectTransform parent, string name, float x, float y, float w, float h)
    {
        var rt = NewRect(name, parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }
}
