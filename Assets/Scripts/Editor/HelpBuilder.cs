using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static ActionCardBuilder;

// "BioValue/Build Help": the "How to play" screen (mockup "How to play") and the "?" button, as
// Resources/HelpScreen.prefab and Resources/HelpButton.prefab. HelpScreen fills in the text;
// HelpButton adds the "?" to the scenes at runtime. The main menu's "How to play" is built by
// "BioValue/Build Main Menu".
// "BioValue/Build Main Game Background": the main game's background in the menu's look (mockup
// "Main game background", option A · Night).
public static class HelpBuilder
{
    private const string ScreenPath = "Assets/Resources/HelpScreen.prefab";
    private const string ButtonPath = "Assets/Resources/HelpButton.prefab";
    private const string GameScenePath = "Assets/Scenes/MainGame.unity";

    private static readonly Color Pink = new Color32(0xFE, 0x49, 0xA4, 0xFF);
    private static readonly Color Purple = new Color32(0x54, 0x34, 0xC8, 0xFF);
    private static readonly Color DeepPurple = new Color32(0x46, 0x32, 0x7F, 0xFF);
    private static readonly Color Ink = new Color32(0x24, 0x1B, 0x3A, 0xFF);
    private static readonly Color Muted = new Color32(0x6B, 0x65, 0x84, 0xFF);
    private static readonly Color Body = new Color32(0x4A, 0x44, 0x60, 0xFF);
    private static readonly Color Line = new Color32(0xDD, 0xD6, 0xEA, 0xFF);
    private static readonly Color Lilac = new Color32(0xF1, 0xEC, 0xFB, 0xFF);
    private static readonly Color Disabled = new Color32(0xF1, 0xEF, 0xF6, 0xFF);
    private static readonly Color FooterBg = new Color32(0xFC, 0xFB, 0xFE, 0xFF);
    private static readonly Color OkBg = new Color32(0xDD, 0xF3, 0xE0, 0xFF);
    private static readonly Color OkText = new Color32(0x1E, 0x7A, 0x2C, 0xFF);
    private static readonly Color PinkBg = new Color32(0xFF, 0xE3, 0xF1, 0xFF);
    private static readonly Color PinkText = new Color32(0x8A, 0x14, 0x50, 0xFF);
    private static readonly Color Grey = new Color32(0x9A, 0x92, 0xAC, 0xFF);
    private static readonly Color Amber = new Color32(0xC9, 0x7A, 0x12, 0xFF);
    private static readonly Color OpenPink = new Color32(0xFE, 0x46, 0xA5, 0xFF);
    private static readonly Color Cream = new Color32(0xFF, 0xFB, 0xF2, 0xFF);
    private static readonly Color Orange = new Color32(0xFD, 0xAA, 0x12, 0xFF);
    private static readonly Color Blue = new Color32(0x25, 0x7A, 0xFE, 0xFF);
    private static readonly Color CollabBlue = new Color32(0x2F, 0x6F, 0xB0, 0xFF);
    private static readonly Color Teal = new Color32(0x22, 0x8A, 0xA2, 0xFF);
    private static readonly Color Gold = new Color32(0xFF, 0xD5, 0x6B, 0xE6);

    private const float PageW = 1402, PageH = 915, FooterH = 96, ArtW = 560;

    [MenuItem("BioValue/Build Help")]
    public static void Build()
    {
        if (!MainMenuBuilder.Ready("Build Help")) return;
        BuildScreen();
        BuildButton();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Build Help",
            "Built the How to play screen and the ? button (Assets/Resources).\n\n" +
            "Run BioValue/Build Main Menu too, for the How to play button on the menu.", "OK");
    }

    // ── The screen ───────────────────────────────────────────────────────────

    private static void BuildScreen()
    {
        var root = new GameObject("HelpScreen", typeof(RectTransform)).GetComponent<RectTransform>();
        Stretch(root);
        var s = root.gameObject.AddComponent<HelpScreen>();
        var block = root.gameObject.AddComponent<Image>();
        block.color = Color.clear; // nothing behind it is clickable
        MainMenuBuilder.Backdrop(root, faint: true);

        // Top bar
        var logo = MainMenuBuilder.Logo(root);
        Place(logo, 43, 29, 227, 227 * 1596f / 5318f);
        LobbyBuilder.Label(root, "Title", bold, 37, Color.white, 290, 29, 400, 68, TextAlignmentOptions.MidlineLeft).text = "How to play";

        var close = NewRect("Close", root);
        close.anchorMin = close.anchorMax = close.pivot = new Vector2(1, 1);
        close.anchoredPosition = new Vector2(-43, -34);
        close.sizeDelta = new Vector2(61, 61);
        s.closeButton = MainMenuBuilder.OutlinedButton(close, "", 20, 61);
        MainHudBuilder.Cross(close, Color.white, 0.9f);

        var roles = NewRect("Role", root);
        roles.anchorMin = roles.anchorMax = roles.pivot = new Vector2(1, 1);
        roles.anchoredPosition = new Vector2(-43 - 61 - 21, -37);
        roles.sizeDelta = new Vector2(370, 56);
        Round(roles, new Color(1, 1, 1, .14f), 28);
        string[] roleNames = { "Player", "Game master" };
        float[] roleWidths = { 140, 222 };
        s.roleButtons = new Button[2];
        float rx = 4;
        for (int i = 0; i < 2; i++)
        {
            var b = Place(NewRect(roleNames[i], roles), rx, 4, roleWidths[i], 48);
            rx += roleWidths[i];
            var img = Round(b, Color.clear, 24);
            img.raycastTarget = true;
            var button = b.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            MainMenuBuilder.Tints(button);
            Txt(Stretch(NewRect("Label", b)), bold, 21, Color.white, TextAlignmentOptions.Center).text = roleNames[i];
            s.roleButtons[i] = button;
        }

        // Chapters
        var nav = Place(NewRect("Chapters", root), 43, 133, 400, 915);
        Round(nav, new Color(1, 1, 1, .1f), 32);
        int count = HelpScreen.Chapters.Length;
        s.chapterButtons = new Button[count];
        for (int i = 0; i < count; i++)
        {
            var ch = HelpScreen.Chapters[i];
            var item = Place(NewRect("Chapter " + (i + 1), nav), 19, 24 + i * 94, 362, 86);
            var bg = Round(item, Color.clear, 21);
            bg.raycastTarget = true;
            var button = item.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            MainMenuBuilder.Tints(button);
            var hx = Place(NewRect("Hex", item), 16, 12, 53, 61);
            var hxImg = hx.gameObject.AddComponent<Image>();
            hxImg.sprite = hex;
            hxImg.raycastTarget = false;
            Txt(Stretch(NewRect("Number", hx)), bold, 23, Color.white, TextAlignmentOptions.Center).text = (i + 1).ToString();
            var title = LobbyBuilder.Label(item, "Title", bold, 24, Color.white, 85, 12, 265, 32, TextAlignmentOptions.MidlineLeft);
            title.text = ch.title;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.enableAutoSizing = true;
            title.fontSizeMin = 18;
            title.fontSizeMax = 24;
            var sub = LobbyBuilder.Label(item, "Sub", medium, 19, Color.white, 85, 44, 265, 28, TextAlignmentOptions.MidlineLeft);
            sub.text = ch.sub;
            sub.textWrappingMode = TextWrappingModes.NoWrap;
            sub.overflowMode = TextOverflowModes.Ellipsis;
            s.chapterButtons[i] = button;
        }

        // Page
        var page = Place(NewRect("Page", root), 475, 133, PageW, PageH);
        var pageImg = Round(page, Color.white, 32);
        page.gameObject.AddComponent<Mask>(); // the picture panel follows the rounded corners
        var shadow = page.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.04f, 0.02f, 0.16f, 0.25f);
        shadow.effectDistance = new Vector2(0, -8);

        var art = Place(NewRect("Art", page), 0, 0, ArtW, PageH - FooterH);
        s.artFill = art.gameObject.AddComponent<Image>();
        s.artFill.raycastTarget = false;
        s.arts = new[]
        {
            ArtGoal(art), ArtRole(art), ArtInstruments(art), ArtCards(art), ArtCollaboration(art), ArtResults(art), ArtGameMaster(art)
        };

        BuildText(page, s);
        BuildFooter(page, s, count);

        PrefabUtility.SaveAsPrefabAsset(root.gameObject, ScreenPath);
        Object.DestroyImmediate(root.gameObject);
    }

    private static void BuildText(RectTransform page, HelpScreen s)
    {
        var col = Place(NewRect("Text", page), ArtW + 50, 45, PageW - ArtW - 50 - 53, PageH - FooterH - 45 - 30);
        var layout = col.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        s.textColumn = col;

        s.eyebrow = Txt(NewRect("Eyebrow", col), bold, 20, Pink, TextAlignmentOptions.TopLeft);
        s.eyebrow.characterSpacing = 10;
        s.eyebrow.text = "WELCOME";
        s.title = Txt(NewRect("Title", col), bold, 50, Ink, TextAlignmentOptions.TopLeft);
        s.title.lineSpacing = -14;
        s.title.text = "Bring biodiversity into the city, together";
        s.lead = Txt(NewRect("Lead", col), regular, 25, Body, TextAlignmentOptions.TopLeft);
        s.lead.text = "Lead";

        var steps = NewRect("Steps", col);
        var stepsLayout = steps.gameObject.AddComponent<VerticalLayoutGroup>();
        stepsLayout.spacing = 16;
        stepsLayout.padding = new RectOffset(0, 0, 16, 0);
        stepsLayout.childControlWidth = stepsLayout.childControlHeight = true;
        stepsLayout.childForceExpandWidth = true;
        stepsLayout.childForceExpandHeight = false;
        s.steps = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            var row = NewRect("Step " + (i + 1), steps);
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 18;
            rowLayout.childAlignment = TextAnchor.UpperLeft;
            rowLayout.childControlWidth = rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = false;
            var badge = NewRect("Badge", row);
            var badgeSize = badge.gameObject.AddComponent<LayoutElement>();
            badgeSize.minWidth = badgeSize.preferredWidth = badgeSize.minHeight = badgeSize.preferredHeight = 43;
            Round(badge, Pink, 21.5f);
            Txt(Stretch(NewRect("Number", badge)), bold, 21, Color.white, TextAlignmentOptions.Center).text = (i + 1).ToString();
            var text = Txt(NewRect("Text", row), regular, 24, Ink, TextAlignmentOptions.TopLeft);
            text.text = "Step";
            text.margin = new Vector4(0, 3, 0, 0);
            text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            s.steps[i] = row.gameObject;
        }

        NewRect("Spacer", col).gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

        var tip = NewRect("Tip", col);
        Round(tip, new Color32(0xFF, 0xF6, 0xE3, 0xFF), 19);
        var tipLayout = tip.gameObject.AddComponent<HorizontalLayoutGroup>();
        tipLayout.padding = new RectOffset(21, 21, 16, 16);
        tipLayout.spacing = 16;
        tipLayout.childAlignment = TextAnchor.UpperLeft;
        tipLayout.childControlWidth = tipLayout.childControlHeight = true;
        tipLayout.childForceExpandWidth = tipLayout.childForceExpandHeight = false;
        var pill = NewRect("Pill", tip);
        var pillSize = pill.gameObject.AddComponent<LayoutElement>();
        pillSize.minWidth = pillSize.preferredWidth = 60;
        pillSize.minHeight = pillSize.preferredHeight = 30;
        Round(pill, Orange, 15);
        Txt(Stretch(NewRect("Label", pill)), bold, 19, Ink, TextAlignmentOptions.Center).text = "Tip";
        s.tipText = Txt(NewRect("Text", tip), regular, 21, new Color32(0x5A, 0x45, 0x20, 0xFF), TextAlignmentOptions.TopLeft);
        s.tipText.text = "Tip";
        s.tipText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        s.tip = tip.gameObject;
    }

    private static void BuildFooter(RectTransform page, HelpScreen s, int count)
    {
        var footer = Place(NewRect("Footer", page), 0, PageH - FooterH, PageW, FooterH);
        var bg = footer.gameObject.AddComponent<Image>();
        bg.color = FooterBg;
        bg.raycastTarget = false;
        Round(Place(NewRect("Line", footer), 0, 0, PageW, 3), Disabled, 1.5f);

        s.backButton = LobbyBuilder.ChipButton(Place(NewRect("Back", footer), 32, 14, 140, 69), "Back", 24, 34.5f);

        var next = NewRect("Next", footer);
        next.anchorMin = next.anchorMax = next.pivot = new Vector2(1, 0.5f);
        next.anchoredPosition = new Vector2(-32, 0);
        next.sizeDelta = new Vector2(420, 69);
        s.nextButton = LobbyBuilder.PillButton(next, Pink, "Next", 24, Color.white);
        s.nextLabel = next.Find("Label").GetComponent<TMP_Text>();

        var dots = NewRect("Dots", footer);
        dots.anchorMin = dots.anchorMax = dots.pivot = new Vector2(0.5f, 0.5f);
        dots.sizeDelta = new Vector2(300, 20);
        var row = dots.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 11;
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = false;
        s.dots = new Image[count];
        for (int i = 0; i < count; i++)
        {
            var d = NewRect("Dot", dots);
            var size = d.gameObject.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = 13;
            size.minHeight = size.preferredHeight = 13;
            s.dots[i] = Round(d, Line, 6.5f);
        }
    }

    // ── Pictures, one per chapter (in the 560 × 819 panel) ───────────────────

    private static RectTransform ArtRoot(RectTransform art, int n)
    {
        var rt = Stretch(NewRect("Art " + n, art));
        rt.gameObject.SetActive(n == 1);
        return rt;
    }

    private static GameObject ArtGoal(RectTransform art)
    {
        var a = ArtRoot(art, 1);
        MiniBoard(a, 220);
        var glow = Place(NewRect("Glow", a), 195, 269, 170, 196).gameObject.AddComponent<Image>();
        glow.sprite = Sprite("Assets/Images/Objective/SoftGlow.png");
        glow.color = Gold;
        glow.raycastTarget = false;
        glow.gameObject.AddComponent<PlayerTurnGlow>().minAlpha = 0.45f;
        var objective = Place(NewRect("Objective", a), 237, 318, 86, 98).gameObject.AddComponent<Image>();
        objective.sprite = Sprite("Assets/Images/Objective/RainbowHex.png");
        objective.raycastTarget = false;
        Caption(a, "The objective lands in the middle of the board");
        return a.gameObject;
    }

    private static GameObject ArtRole(RectTransform art)
    {
        var a = ArtRoot(art, 2);
        var card = WhiteCard(a, 80, 225, 400, 345);
        Circle(card, "Avatar", 26, 26, 59, Blue, "2", Color.white);
        Text(card, "Name", bold, 25, Ink, 100, 24, 270, 34, "Ana");
        Text(card, "Entity", regular, 19, Muted, 100, 56, 270, 28, "City council");
        Text(card, "Eyebrow", bold, 17, Muted, 26, 110, 348, 26, "MY OBJECTIVE").characterSpacing = 8;
        Text(card, "Objective", regular, 23, Ink, 26, 138, 348, 90, "Green the school yards of my district by 2030.");
        Chip(card, "Ready", 26, 264, 348, 54, Pink, "I'm ready", Color.white, 22);
        return a.gameObject;
    }

    private static GameObject ArtInstruments(RectTransform art)
    {
        var a = ArtRoot(art, 3);
        MiniBoard(a, 190);
        float x = 280 + 1.5f * 69 - 30.5f, y = 190 + 56; // second row, last cell
        Hexagon(a, "Piece", x - 3, y - 4, 67, 77, Teal);
        Hexagon(a, "PieceFace", x + 5, y + 5, 51, 59, Color.white);
        var vote = WhiteCard(a, 90, 520, 380, 60, 30);
        Text(vote, "Ask", bold, 20, Ink, 22, 0, 170, 60, "Do you agree?", TextAlignmentOptions.MidlineLeft);
        Chip(vote, "Yes", 200, 13, 70, 34, OkBg, "Yes", OkText, 18);
        Chip(vote, "No", 282, 13, 70, 34, PinkBg, "No", PinkText, 18);
        return a.gameObject;
    }

    private static GameObject ArtCards(RectTransform art)
    {
        var a = ArtRoot(art, 4);
        string[] names = { "Changing the way I work to enhance biodiversity", "Creating partnerships to enhance biodiversity", "Open action: community gardens" };
        float[] xs = { 80, 180, 280 }, turns = { 10, 0, -10 };
        for (int i = 0; i < 3; i++)
        {
            bool open = i == 2;
            Color accent = open ? OpenPink : Amber;
            var card = Place(NewRect("Card " + (i + 1), a), xs[i], 210, 200, 253);
            card.localEulerAngles = new Vector3(0, 0, turns[i]);
            Round(card, accent, 19);
            LobbyBuilder.Inset(card, "Fill", Cream, 15, 4);
            Hexagon(card, "Hex", 16, 16, 40, 46, accent);
            Text(card, "Title", bold, 20, Ink, 16, 72, 168, 160, names[i]).lineSpacing = -10;
        }
        Caption(a, "Link each card to instruments on the board");
        return a.gameObject;
    }

    private static GameObject ArtCollaboration(RectTransform art)
    {
        var a = ArtRoot(art, 5);
        var card = WhiteCard(a, 60, 215, 440, 260);
        Circle(card, "Avatar", 22, 22, 51, Orange, "3", Ink);
        var ask = Text(card, "Title", bold, 21, Ink, 86, 18, 330, 60, "Rui wants to collaborate on your action", TextAlignmentOptions.MidlineLeft);
        ask.lineSpacing = -12;
        var action = Place(NewRect("Action", card), 22, 92, 396, 74);
        Round(action, OpenPink, 16);
        LobbyBuilder.Inset(action, "Fill", Color.white, 13.5f, 2.5f);
        Text(action, "Text", bold, 19, Ink, 16, 0, 364, 74, "Open action: community gardens on vacant lots", TextAlignmentOptions.MidlineLeft).lineSpacing = -10;
        Chip(card, "Decline", 22, 186, 190, 50, Disabled, "Decline", Ink, 19);
        Chip(card, "Accept", 228, 186, 190, 50, Pink, "Accept", Color.white, 19);
        for (int i = 0; i < 5; i++)
            Circle(a, "Token " + (i + 1), 60 + i * 38, 510, 29, i < 3 ? CollabBlue : new Color32(0xDC, 0xE6, 0xF2, 0xFF), "", Color.white);
        Text(a, "TokensLeft", bold, 20, CollabBlue, 60 + 5 * 38 + 8, 505, 200, 40, "3 tokens left", TextAlignmentOptions.MidlineLeft);
        return a.gameObject;
    }

    private static GameObject ArtResults(RectTransform art)
    {
        var a = ArtRoot(art, 6);
        var card = WhiteCard(a, 60, 215, 440, 250);
        var q1 = Text(card, "Question 1", bold, 16, Muted, 24, 22, 392, 24, "HOW CLOSE ARE WE TO THE GAME OBJECTIVE?");
        q1.characterSpacing = 4;
        q1.enableAutoSizing = true;
        q1.fontSizeMin = 12;
        q1.fontSizeMax = 16;
        for (int i = 0; i < 5; i++) Circle(card, "Rating 1." + (i + 1), 24 + i * 50, 56, 40, i < 4 ? Orange : new Color32(0xEE, 0xEA, 0xF5, 0xFF), "", Color.white);
        Text(card, "Question 2", bold, 16, Muted, 24, 122, 392, 24, "AND TO YOUR OWN?").characterSpacing = 4;
        for (int i = 0; i < 5; i++) Circle(card, "Rating 2." + (i + 1), 24 + i * 50, 156, 40, i < 3 ? Orange : new Color32(0xEE, 0xEA, 0xF5, 0xFF), "", Color.white);
        var formula = Place(NewRect("Formula", a), 60, 500, 440, 96);
        Round(formula, new Color32(0xF6, 0xF2, 0xFC, 0xFF), 16);
        Text(formula, "Text", bold, 19, DeepPurple, 16, 0, 408, 96,
             "Score = game objective × your objective + 2 × actions + collaborations", TextAlignmentOptions.Center);
        return a.gameObject;
    }

    private static GameObject ArtGameMaster(RectTransform art)
    {
        var a = ArtRoot(art, 7);
        var card = WhiteCard(a, 80, 205, 400, 370);
        Text(card, "Title", bold, 27, Ink, 24, 22, 352, 36, "Action cards round");
        string[] names = { "Ana", "Rui", "Marta" };
        Color[] colors = { Blue, Orange, new Color32(0x2B, 0xB3, 0xA3, 0xFF) };
        for (int i = 0; i < 3; i++)
        {
            float y = 80 + i * 56;
            bool played = i < 2;
            Circle(card, "Player " + (i + 1), 24, y, 40, colors[i], (i + 1).ToString(), Color.white);
            Text(card, "Name " + (i + 1), bold, 21, Ink, 76, y, 170, 40, names[i], TextAlignmentOptions.MidlineLeft);
            Chip(card, "Status " + (i + 1), 250, y + 5, 126, 30, played ? OkBg : new Color32(0xEE, 0xEA, 0xF5, 0xFF),
                 played ? "Played" : "Choosing", played ? OkText : Grey, 17);
        }
        var glow = Place(NewRect("Glow", card), 16, 262, 368, 72);
        Round(glow, new Color(Pink.r, Pink.g, Pink.b, .35f), 36);
        glow.gameObject.AddComponent<PlayerTurnGlow>().minAlpha = 0.15f;
        Chip(card, "EndActions", 24, 270, 352, 56, Purple, "End actions round", Color.white, 21);
        return a.gameObject;
    }

    // 19 cells in rows of 3-4-5-4-3, the middle one lilac.
    private static void MiniBoard(RectTransform parent, float top)
    {
        int[] rows = { 3, 4, 5, 4, 3 };
        const float w = 61, h = 70, gap = 8, step = 56, cx = 280;
        var board = Stretch(NewRect("Board", parent));
        for (int r = 0; r < rows.Length; r++)
        for (int i = 0; i < rows[r]; i++)
        {
            float x = cx + (i - (rows[r] - 1) / 2f) * (w + gap) - w / 2, y = top + r * step;
            Hexagon(board, "Edge", x - 2, y - 2, w + 4, h + 4, Line);
            Hexagon(board, "Cell", x, y, w, h, r == 2 && i == 2 ? Lilac : Color.white);
        }
    }

    private static void Caption(RectTransform parent, string text) =>
        Text(parent, "Caption", medium, 21, Body, 40, 600, 480, 34, text, TextAlignmentOptions.Center);

    private static Image Hexagon(RectTransform parent, string name, float x, float y, float w, float h, Color color)
    {
        var img = Place(NewRect(name, parent), x, y, w, h).gameObject.AddComponent<Image>();
        img.sprite = hex;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static RectTransform WhiteCard(RectTransform parent, float x, float y, float w, float h, float radius = 26)
    {
        var rt = Place(NewRect("Card", parent), x, y, w, h);
        Round(rt, Color.white, radius);
        var shadow = rt.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.14f, 0.1f, 0.23f, 0.15f);
        shadow.effectDistance = new Vector2(0, -10);
        return rt;
    }

    private static void Circle(RectTransform parent, string name, float x, float y, float d, Color color, string text, Color textColor)
    {
        var rt = Place(NewRect(name, parent), x, y, d, d);
        Round(rt, color, d / 2);
        if (!string.IsNullOrEmpty(text))
            Txt(Stretch(NewRect("Number", rt)), bold, d * 0.45f, textColor, TextAlignmentOptions.Center).text = text;
    }

    private static void Chip(RectTransform parent, string name, float x, float y, float w, float h, Color bg, string text, Color fg, float size)
    {
        var rt = Place(NewRect(name, parent), x, y, w, h);
        Round(rt, bg, h / 2);
        Txt(Stretch(NewRect("Label", rt)), bold, size, fg, TextAlignmentOptions.Center).text = text;
    }

    private static TextMeshProUGUI Text(RectTransform parent, string name, TMP_FontAsset font, float size, Color color,
                                        float x, float y, float w, float h, string text,
                                        TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
    {
        var t = LobbyBuilder.Label(parent, name, font, size, color, x, y, w, h, align);
        t.text = text;
        return t;
    }

    private static Sprite Sprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (!sprite) Debug.LogWarning("[HelpBuilder] Missing " + path + ": run BioValue/Build Objective Piece first.");
        return sprite;
    }

    // ── The "?" button ───────────────────────────────────────────────────────

    private static void BuildButton()
    {
        var root = new GameObject("HelpButton", typeof(RectTransform)).GetComponent<RectTransform>();
        root.sizeDelta = new Vector2(56, 56);
        var fill = Round(root, new Color(1, 1, 1, .16f), 28);
        fill.raycastTarget = true;
        var ring = Stretch(NewRect("Outline", root)).gameObject.AddComponent<Image>();
        ring.sprite = MainMenuBuilder.RoundRing();
        ring.type = Image.Type.Sliced;
        ring.pixelsPerUnitMultiplier = 24f / 28f;
        ring.color = new Color(1, 1, 1, .45f);
        ring.raycastTarget = false;
        Txt(Stretch(NewRect("Label", root)), bold, 30, Color.white, TextAlignmentOptions.Center).text = "?";
        var button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = fill;
        MainMenuBuilder.Tints(button);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, ButtonPath);
        Object.DestroyImmediate(root.gameObject);
    }

    // ── Main game background ─────────────────────────────────────────────────

    [MenuItem("BioValue/Build Main Game Background")]
    public static void BuildBackground()
    {
        if (!MainMenuBuilder.Ready("Build Main Game Background") || !MainMenuBuilder.Open(GameScenePath, out var scene)) return;

        Image background = null;
        foreach (var img in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (img.gameObject.scene != scene || img.name != "Background") continue;
            var parent = img.transform.parent;
            if (parent && parent.GetComponent<Canvas>() && parent.parent == null) { background = img; break; }
        }
        if (!background)
        {
            EditorUtility.DisplayDialog("Build Main Game Background", "Couldn't find the Background under the main Canvas.", "OK");
            return;
        }

        // The menu's radial purple, a soft light behind the board, a few brand hexes drifting at the edges.
        background.sprite = MainMenuBuilder.Gradient();
        background.type = Image.Type.Simple;
        background.color = Color.white;
        var bgRT = background.rectTransform;
        for (int i = bgRT.childCount - 1; i >= 0; i--)
        {
            var child = bgRT.GetChild(i);
            if (child.name == "Light" || child.name == "Hexes") Object.DestroyImmediate(child.gameObject);
        }
        var light = NewRect("Light", bgRT);
        light.anchorMin = light.anchorMax = light.pivot = new Vector2(0.5f, 0.52f);
        light.sizeDelta = new Vector2(1500, 1000);
        var lightImg = light.gameObject.AddComponent<Image>();
        lightImg.sprite = Sprite("Assets/Images/Objective/SoftGlow.png");
        lightImg.color = new Color(1, 1, 1, .12f);
        lightImg.raycastTarget = false;

        var hexes = Stretch(NewRect("Hexes", bgRT));
        MainMenuBuilder.Hex(hexes, 70, 150, 80, Pink, .28f, -12, 0f);
        MainMenuBuilder.Hex(hexes, 170, 600, 56, Orange, .3f, 8, 1.2f);
        MainMenuBuilder.Hex(hexes, 1290, 120, 72, Blue, .3f, 14, .6f);
        MainMenuBuilder.Hex(hexes, 1280, 560, 100, Orange, .22f, -6, 1.8f);
        MainMenuBuilder.Hex(hexes, 60, 380, 40, Blue, .3f, 10, 2.6f);
        MainMenuBuilder.Hex(hexes, 1370, 360, 44, Pink, .28f, -8, 3.1f);
        MainMenuBuilder.Hex(hexes, 300, 90, 30, Color.white, .14f, 6, 1.5f);

        EditorUtility.SetDirty(background);
        MainMenuBuilder.Save(scene, "Main game background built");
    }
}
