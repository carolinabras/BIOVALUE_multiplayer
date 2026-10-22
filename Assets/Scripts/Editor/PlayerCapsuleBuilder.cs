using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static ActionCardBuilder;

// Player capsules: a white pill with a profile icon (a person on a circle in the player's colour,
// the player number in a small corner badge) and the name, nothing else. A pink outline breathes on
// the current player's turn. Clicking a capsule opens the player details window (organisation, tokens,
// instruments placed, action cards played), which this also builds on MainGame's canvas.
// Safe to run again: rebuilds the capsule's "Look" and the details window.
public static class PlayerCapsuleBuilder
{
    private const string PrefabPath = "Assets/Resources/PhotonPrefabs/PlayerCapsule.prefab";
    private const string ScenePath = "Assets/Scenes/MainGame.unity";
    private const string BoardPrefabPath = "Assets/Resources/PhotonPrefabs/PlayersBoard.prefab";

    // Details window: player on the left (their colour), tabs on the right.
    private const float W = 1000f, H = 670f, LeftW = 320f, StatH = 78f, TabsH = 66f;

    private static readonly Color Purple = new Color32(0x46, 0x32, 0x7F, 0xFF);
    private static readonly Color PurpleMid = new Color32(0x6A, 0x55, 0xA8, 0xFF);
    private static readonly Color Lilac = new Color32(0xF6, 0xF2, 0xFC, 0xFF);
    private static readonly Color Pink = new Color32(0xFE, 0x46, 0xA5, 0xFF);
    private static readonly Color PinkLight = new Color32(0xFF, 0xE3, 0xF1, 0xFF);
    private static readonly Color PinkDark = new Color32(0x8A, 0x14, 0x50, 0xFF);
    private static readonly Color Orange = new Color32(0xB5, 0x65, 0x0A, 0xFF);
    private static readonly Color Cream = new Color32(0xFB, 0xF0, 0xDF, 0xFF);
    private static readonly Color Ink = new Color32(0x24, 0x1B, 0x3A, 0xFF);
    private static readonly Color Muted = new Color32(0x5E, 0x55, 0x75, 0xFF);
    private static readonly Color Yes = new Color32(0x2F, 0xA8, 0x4F, 0xFF);
    private static readonly Color No = new Color32(0xD9, 0x3A, 0x3A, 0xFF);
    private static readonly Color CardCream = new Color32(0xFF, 0xFB, 0xF2, 0xFF);
    private static readonly Color Body = new Color32(0x4A, 0x44, 0x60, 0xFF);
    private static readonly Color TokenBlue = new Color32(0x2F, 0x6F, 0xB0, 0xFF);

    // Round token with a white ring and a tick or a cross, inside the capsule's right end.
    private static GameObject VoteToken(RectTransform look, string name, Color color, bool tick)
    {
        var token = NewRect(name, look);
        token.anchorMin = token.anchorMax = token.pivot = new Vector2(1, 0.5f);
        token.anchoredPosition = new Vector2(-7, 0);
        token.sizeDelta = new Vector2(38, 38);
        Round(token, Color.white, 19);
        var fill = Stretch(NewRect("Fill", token));
        fill.offsetMin = new Vector2(3, 3);
        fill.offsetMax = new Vector2(-3, -3);
        Round(fill, color, 16);
        if (tick)
        {
            TokenBar(fill, new Vector2(-4.5f, -1.5f), new Vector2(9, 4), -45f);
            TokenBar(fill, new Vector2(2.6f, 1.6f), new Vector2(17, 4), 50f);
        }
        else
        {
            TokenBar(fill, Vector2.zero, new Vector2(17, 4), 45f);
            TokenBar(fill, Vector2.zero, new Vector2(17, 4), -45f);
        }
        token.gameObject.SetActive(false);
        return token.gameObject;
    }

    private static void TokenBar(RectTransform parent, Vector2 pos, Vector2 size, float angle)
    {
        var bar = NewRect("Bar", parent);
        bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0.5f, 0.5f);
        bar.anchoredPosition = pos;
        bar.sizeDelta = size;
        bar.localRotation = Quaternion.Euler(0, 0, angle);
        Round(bar, Color.white, size.y / 2f);
    }

    [MenuItem("BioValue/Build Player Capsules")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Build Player Capsules", "Stop Play mode first: the builder edits the scene and prefabs.", "OK");
            return;
        }
        if (!LoadArt())
        {
            EditorUtility.DisplayDialog("Build Player Capsules",
                "Missing art or font. Run BioValue/Build Instrument Piece and BioValue/Build Action Card first.", "OK");
            return;
        }

        BuildCapsule();
        MakeCapsulesUpright();

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Canvas canvas = null;
        foreach (var hook in Object.FindObjectsByType<PlayerCapsuleHook>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (hook.gameObject.scene == scene) { canvas = hook.GetComponentInParent<Canvas>(true); break; }
        if (!canvas)
        {
            EditorUtility.DisplayDialog("Build Player Capsules", "No player capsules found in MainGame.", "OK");
            return;
        }
        var root = canvas.rootCanvas.transform;
        Transform old;
        while ((old = root.Find("PlayerDetailsPanel")) != null) Object.DestroyImmediate(old.gameObject);
        BuildPanel(root);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[PlayerCapsuleBuilder] Player capsules restyled and details window built in MainGame (scene saved).");
    }

    // ── Capsule ──────────────────────────────────────────────────────────────

    private static void BuildCapsule()
    {
        var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var capsule = (RectTransform)contents.transform;
            var hook = contents.GetComponent<PlayerCapsuleHook>();

            // Old look: orange pill, "Player N" tag, separate cards button. The vote marks stay.
            foreach (var name in new[] { "Look", "PlayerTag", "Image", "NotMyTurnImageOverlay", "PlayerNameText", "Button" })
            {
                Transform t;
                while ((t = capsule.Find(name)) != null) Object.DestroyImmediate(t.gameObject);
            }
            var oldBack = contents.GetComponent<Image>();
            if (oldBack) oldBack.enabled = false;

            var look = Stretch(NewRect("Look", capsule));
            look.SetAsFirstSibling();

            var glow = Stretch(NewRect("TurnGlow", look));
            glow.offsetMin = new Vector2(-5, -5);
            glow.offsetMax = new Vector2(5, 5);
            Round(glow, Pink, 36);
            glow.gameObject.AddComponent<PlayerTurnGlow>();
            glow.gameObject.SetActive(false);

            var shadow = Stretch(NewRect("Shadow", look));
            shadow.offsetMin = new Vector2(0, -4);
            shadow.offsetMax = new Vector2(0, -4);
            Round(shadow, new Color(0.06f, 0.03f, 0.14f, 0.25f), 32);

            var pill = Stretch(NewRect("Pill", look));
            var pillImg = Round(pill, Color.white, 32);
            pillImg.raycastTarget = true;

            // Profile picture: a person on a circle in the player's colour, the number in a small corner badge.
            var badge = NewRect("Avatar", look);
            badge.anchorMin = badge.anchorMax = badge.pivot = new Vector2(0, 0.5f);
            badge.anchoredPosition = new Vector2(7, 0);
            badge.sizeDelta = new Vector2(52, 52);
            var badgeImg = Circle(badge, PlayerCapsuleHook.PlayerColors[0], out var circle);
            var person = Stretch(NewRect("Person", circle));
            person.offsetMin = new Vector2(8, 0);
            person.offsetMax = new Vector2(-8, -8);
            Icon(person, GetOrCreatePerson(), Color.white);
            var numberDot = NumberBadge(badge, 22, Color.white, PlayerCapsuleHook.PlayerColors[0], 14, out var number);

            var nameRT = Stretch(NewRect("Name", look));
            nameRT.offsetMin = new Vector2(70, 4);
            nameRT.offsetMax = new Vector2(-50, -4); // room for the vote token on the right
            var nameText = Txt(nameRT, bold, 24, Ink, TextAlignmentOptions.MidlineLeft);
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 16;
            nameText.fontSizeMax = 24;
            nameText.text = "Player name";

            // "NEXT" tag for the player who plays after the current one; the hook puts it above or below.
            var nextTag = NewRect("NextTag", look);
            nextTag.anchorMin = nextTag.anchorMax = new Vector2(0.5f, 1f);
            nextTag.pivot = new Vector2(0.5f, 0f);
            nextTag.sizeDelta = new Vector2(76, 26);
            nextTag.anchoredPosition = new Vector2(0, 6);
            Round(nextTag, Purple, 13);
            var nextLabel = Txt(Stretch(NewRect("Label", nextTag)), bold, 15, Color.white, TextAlignmentOptions.Center);
            nextLabel.characterSpacing = 8;
            nextLabel.textWrappingMode = TextWrappingModes.NoWrap;
            nextLabel.text = "NEXT";
            nextTag.gameObject.SetActive(false);

            // Vote token at the right end: green tick (agrees) or red cross (disagrees), shown while a
            // vote is open once this player has voted. Replaces the old approve / reject marks.
            var voteYes = VoteToken(look, "VoteYes", Yes, true);
            var voteNo = VoteToken(look, "VoteNo", No, false);
            foreach (var name in new[] { "AproveIndicator", "RejectIndicator" }) // the old marks
            {
                Transform t;
                while ((t = capsule.Find(name)) != null) Object.DestroyImmediate(t.gameObject);
            }

            var so = new SerializedObject(hook);
            so.FindProperty("_playerName").objectReferenceValue = nameText;
            so.FindProperty("_playerID").objectReferenceValue = number;
            so.FindProperty("_badge").objectReferenceValue = badgeImg;
            var tinted = so.FindProperty("_playerColored");
            tinted.arraySize = 2;
            tinted.GetArrayElementAtIndex(0).objectReferenceValue = number;   // number on the white corner badge
            tinted.GetArrayElementAtIndex(1).objectReferenceValue = numberDot.GetComponent<Outline>();
            so.FindProperty("_turnGlow").objectReferenceValue = glow.gameObject;
            so.FindProperty("_nextTag").objectReferenceValue = nextTag;
            so.FindProperty("_approveIndicator").objectReferenceValue = voteYes;
            so.FindProperty("_rejectIndicator").objectReferenceValue = voteNo;
            so.FindProperty("_capsuleImage").objectReferenceValue = null;
            so.FindProperty("_notMyTurnOverlay").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();

            var activate = contents.GetComponent<ActivateActionHandButton>();
            if (activate)
            {
                var aso = new SerializedObject(activate);
                aso.FindProperty("button").objectReferenceValue = null;
                aso.ApplyModifiedPropertiesWithoutUndo();
            }

            // The whole capsule is the button: it opens the details window.
            var button = contents.GetComponent<Button>();
            if (!button) button = contents.AddComponent<Button>();
            button.targetGraphic = pillImg;
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddPersistentListener(button.onClick, new UnityAction(hook.OpenDetails));

            PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    // The top row's container is turned 180° (the old capsules turned their text back). Turn each
    // capsule back instead of the container, so they stay exactly where they are but read upright.
    private static void MakeCapsulesUpright()
    {
        var contents = PrefabUtility.LoadPrefabContents(BoardPrefabPath);
        try
        {
            var board = contents.transform;
            foreach (var hook in contents.GetComponentsInChildren<PlayerCapsuleHook>(true))
                hook.transform.rotation = board.rotation;
            PrefabUtility.SaveAsPrefabAsset(contents, BoardPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    // ── Details window ───────────────────────────────────────────────────────

    private static void BuildPanel(Transform canvas)
    {
        var holder = Stretch(NewRect("PlayerDetailsPanel", canvas));
        holder.SetAsLastSibling();
        var panel = holder.gameObject.AddComponent<PlayerDetailsPanel>();

        var root = Stretch(NewRect("Root", holder));
        var group = root.gameObject.AddComponent<CanvasGroup>();

        // Dimmed board; a tap outside the window closes it.
        var dim = Stretch(NewRect("Dim", root)).gameObject.AddComponent<Image>();
        dim.color = new Color(0.08f, 0.047f, 0.196f, 0.6f);
        var dimButton = dim.gameObject.AddComponent<Button>();
        dimButton.transition = Selectable.Transition.None;

        var window = NewRect("Window", root);
        window.anchorMin = window.anchorMax = window.pivot = new Vector2(0.5f, 0.5f);
        window.sizeDelta = new Vector2(W, H);
        var windowImg = Round(window, Color.white, 26);
        windowImg.raycastTarget = true; // clicks on the window don't close it
        var windowShadow = window.gameObject.AddComponent<Shadow>();
        windowShadow.effectColor = new Color(0.04f, 0.02f, 0.16f, 0.45f);
        windowShadow.effectDistance = new Vector2(0, -14);
        window.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        // ── Left: who, in the player's colour ──
        var left = NewRect("Left", window);
        left.anchorMin = Vector2.zero; left.anchorMax = new Vector2(0, 1);
        left.pivot = new Vector2(0, 0.5f);
        left.sizeDelta = new Vector2(LeftW, 0);
        left.anchoredPosition = Vector2.zero;
        var leftFill = left.gameObject.AddComponent<Image>();
        leftFill.color = PlayerCapsuleHook.PlayerColors[0];
        leftFill.raycastTarget = false;
        var shade = Stretch(NewRect("Shade", left));
        shade.anchorMax = new Vector2(1, 0.45f); // a darker lower part, behind the numbers
        var shadeImg = shade.gameObject.AddComponent<Image>();
        shadeImg.color = new Color(0.08f, 0.05f, 0.2f, 0.22f);
        shadeImg.raycastTarget = false;
        var cascade = Stretch(NewRect("HexCascade", left)).gameObject.AddComponent<CardCascade>();
        cascade.cardSprite = hex;
        cascade.pixelsPerUnitMultiplier = 1f;
        cascade.cardSize = new Vector2(26, 30);
        cascade.color = new Color(1f, 1f, 1f, 0.1f);

        var prev = CircleButton(left, "Prev", 20, 20, 40, new Color(1, 1, 1, 0.25f), Color.white, 180f);
        var position = Txt(Place(NewRect("Position", left), 64, 20, LeftW - 128, 40), bold, 14, Color.white, TextAlignmentOptions.Center);
        position.characterSpacing = 6;
        position.textWrappingMode = TextWrappingModes.NoWrap;
        position.text = "PLAYER 1 OF 4";
        var next = CircleButton(left, "Next", LeftW - 60, 20, 40, new Color(1, 1, 1, 0.25f), Color.white, 0f);

        // Avatar: white circle, person in the player's colour, number badge on the edge.
        var avatar = Place(NewRect("Avatar", left), (LeftW - 110) / 2f, 78, 110, 110);
        Circle(avatar, Color.white, out var circle);
        var person = Stretch(NewRect("Person", circle));
        person.offsetMin = new Vector2(18, 0);
        person.offsetMax = new Vector2(-18, -16);
        var avatarIcon = Icon(person, GetOrCreatePerson(), PlayerCapsuleHook.PlayerColors[0]);
        var badge = NewRect("Badge", avatar);
        badge.anchorMin = badge.anchorMax = badge.pivot = new Vector2(1, 0);
        badge.sizeDelta = new Vector2(40, 40);
        badge.anchoredPosition = new Vector2(4, 0);
        var badgeFill = Round(badge, PlayerCapsuleHook.PlayerColors[0], 20);
        var badgeRing = badge.gameObject.AddComponent<Outline>();
        badgeRing.effectColor = Color.white;
        badgeRing.effectDistance = new Vector2(2.5f, -2.5f);
        var badgeNumber = Txt(Stretch(NewRect("Number", badge)), bold, 19, Color.white, TextAlignmentOptions.Center);
        badgeNumber.text = "1";

        // Name, organisation, status: stacked, centred, as tall as they need.
        var who = Place(NewRect("Who", left), 22, 202, LeftW - 44, 230);
        var whoCol = Column(who, 6, new RectOffset(0, 0, 0, 0));
        whoCol.childAlignment = TextAnchor.UpperCenter;
        var nameText = Txt(NewRect("Name", who), bold, 28, Color.white, TextAlignmentOptions.Top);
        nameText.lineSpacing = -12;
        nameText.text = "Player name";
        var company = Txt(NewRect("Company", who), regular, 17, new Color(1, 1, 1, 0.92f), TextAlignmentOptions.Top);
        company.lineSpacing = -8;
        company.text = "Organisation";
        var statusRow = NewRect("StatusRow", who);
        var statusRowLayout = Row(statusRow, 0);
        statusRowLayout.childAlignment = TextAnchor.MiddleCenter;
        statusRowLayout.padding = new RectOffset(0, 0, 6, 0);
        var status = Chip(statusRow, "Status", new Color(0, 0, 0, 0.2f), 32, new RectOffset(14, 16, 0, 0));
        var statusDot = NewRect("Dot", status);
        Fixed(statusDot, 10, 10);
        var statusDotImg = Round(statusDot, Color.white, 5);
        var statusText = ChipLabel(status, bold, 15, Color.white, "Waiting for their turn");

        // Four numbers, two by two, at the bottom.
        var stats = NewRect("Stats", left);
        stats.anchorMin = Vector2.zero; stats.anchorMax = new Vector2(1, 0);
        stats.pivot = new Vector2(0.5f, 0);
        stats.offsetMin = new Vector2(22, 22);
        stats.offsetMax = new Vector2(-22, 22 + 2 * StatH + 8);
        var grid = stats.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2((LeftW - 44 - 8) / 2f, StatH);
        grid.spacing = new Vector2(8, 8);
        var statValues = new TMP_Text[4];
        string[] statLabels = { "instruments placed", "action cards played", "collaborations made", "collaborators on their cards" };
        for (int i = 0; i < 4; i++)
        {
            var tile = NewRect("Stat" + (i + 1), stats);
            Round(tile, new Color(1, 1, 1, 0.16f), 14);
            var value = Txt(Place(NewRect("Value", tile), 12, 8, 110, 30), bold, 28, Color.white, TextAlignmentOptions.TopLeft);
            value.textWrappingMode = TextWrappingModes.NoWrap;
            value.text = "0";
            var label = Txt(Place(NewRect("Label", tile), 12, 40, (LeftW - 52) / 2f - 20, 36), bold, 13, new Color(1, 1, 1, 0.9f), TextAlignmentOptions.TopLeft);
            label.lineSpacing = -14;
            label.text = statLabels[i];
            statValues[i] = value;
        }

        // ── Right: what they did ──
        var right = Stretch(NewRect("Right", window));
        right.offsetMin = new Vector2(LeftW, 0);

        var tabsRT = Place(NewRect("Tabs", right), 20, 16, W - LeftW - 100, TabsH - 16);
        var tabsRow = Row(tabsRT, 4);
        tabsRow.childControlHeight = true;
        tabsRow.childForceExpandHeight = true;
        string[] tabNames = { "Instruments", "Action cards", "Collaborations" };
        float[] tabWidths = { 186, 196, 220 };
        var tabButtons = new Button[3];
        var tabLabels = new TMP_Text[3];
        var tabCountFills = new Image[3];
        var tabCounts = new TMP_Text[3];
        var tabUnderlines = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var tab = NewRect("Tab" + (i + 1), tabsRT);
            Fixed(tab, tabWidths[i], -1);
            var hit = tab.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var button = tab.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            var inner = Row(tab, 8);
            inner.padding = new RectOffset(12, 8, 0, 8);
            inner.childAlignment = TextAnchor.MiddleLeft;
            var label = Txt(NewRect("Label", tab), bold, 18, Ink, TextAlignmentOptions.MidlineLeft);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.text = tabNames[i];
            var count = NewRect("Count", tab);
            Fixed(count, 28, 24);
            var countFill = Round(count, Lilac, 12);
            var countText = Txt(Stretch(NewRect("Text", count)), bold, 14, Muted, TextAlignmentOptions.Center);
            countText.text = "0";
            var underline = NewRect("Underline", tab);
            underline.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            underline.anchorMin = Vector2.zero; underline.anchorMax = new Vector2(1, 0);
            underline.pivot = new Vector2(0.5f, 0);
            underline.sizeDelta = new Vector2(0, 3);
            underline.anchoredPosition = new Vector2(0, -2);
            var underlineImg = underline.gameObject.AddComponent<Image>();
            underlineImg.color = Color.clear;
            underlineImg.raycastTarget = false;
            tabButtons[i] = button; tabLabels[i] = label; tabCountFills[i] = countFill; tabCounts[i] = countText; tabUnderlines[i] = underlineImg;
        }
        var divider = NewRect("Divider", right);
        divider.anchorMin = new Vector2(0, 1); divider.anchorMax = new Vector2(1, 1);
        divider.pivot = new Vector2(0.5f, 1);
        divider.sizeDelta = new Vector2(0, 2);
        divider.anchoredPosition = new Vector2(0, -TabsH);
        divider.gameObject.AddComponent<Image>().color = Lilac;

        var close = NewRect("Close", right);
        close.anchorMin = close.anchorMax = close.pivot = new Vector2(1, 1);
        close.sizeDelta = new Vector2(42, 42);
        close.anchoredPosition = new Vector2(-20, -12);
        var closeImg = Round(close, Lilac, 21);
        closeImg.raycastTarget = true;
        var closeButton = close.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImg;
        foreach (var angle in new[] { 45f, -45f })
        {
            var bar = NewRect("Bar", close);
            bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0.5f, 0.5f);
            bar.sizeDelta = new Vector2(18, 3.5f);
            bar.localRotation = Quaternion.Euler(0, 0, angle);
            Round(bar, Ink, 1.75f);
        }

        // Scrolling pages
        var scroll = Stretch(NewRect("Scroll", right));
        scroll.offsetMin = new Vector2(24, 20);
        scroll.offsetMax = new Vector2(-14, -TabsH - 16);
        var viewport = Stretch(NewRect("Viewport", scroll));
        viewport.gameObject.AddComponent<RectMask2D>();
        viewport.gameObject.AddComponent<Image>().color = Color.clear;
        var content = NewRect("Content", viewport);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = Vector2.zero;
        Column(content, 0, new RectOffset(0, 12, 0, 4));
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var sr = scroll.gameObject.AddComponent<ScrollRect>();
        sr.viewport = viewport;
        sr.content = content;
        sr.horizontal = false;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 40;

        // Page 1: instruments
        var instPage = NewRect("Instruments", content);
        Column(instPage, 10, new RectOffset(0, 0, 0, 0));
        var instRow = NewRect("InstrumentTemplate", instPage);
        var instRowImg = Round(instRow, Lilac, 16);
        instRowImg.raycastTarget = true;
        var instButton = instRow.gameObject.AddComponent<Button>();
        instButton.targetGraphic = instRowImg;
        var instLayout = Row(instRow, 14);
        instLayout.padding = new RectOffset(14, 14, 12, 12);
        var instHex = NewRect("Hex", instRow);
        Fixed(instHex, 52, 60);
        var instHexImg = Icon(instHex, hex, Purple);
        instHexImg.preserveAspect = false;
        var instHexInner = Stretch(NewRect("Inner", instHex));
        instHexInner.offsetMin = new Vector2(6, 7);
        instHexInner.offsetMax = new Vector2(-6, -7);
        Icon(instHexInner, hex, Color.white).preserveAspect = false;
        var instNumber = Txt(Stretch(NewRect("Number", instHex)), bold, 17, Ink, TextAlignmentOptions.Center);
        instNumber.textWrappingMode = TextWrappingModes.NoWrap;
        instNumber.text = "4";
        var instTexts = NewRect("Texts", instRow);
        Column(instTexts, -2, new RectOffset(0, 0, 0, 0));
        instTexts.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        Txt(NewRect("Category", instTexts), bold, 13, Purple, TextAlignmentOptions.TopLeft).text = "Planning & Project Design";
        Txt(NewRect("Name", instTexts), bold, 19, Ink, TextAlignmentOptions.TopLeft).text = "Land Use Zoning";
        var vote = NewRect("Vote", instRow);
        Fixed(vote, 140, 30);
        Round(vote, Lilac, 15);
        var voteLabel = Txt(Stretch(NewRect("Label", vote)), bold, 14, Ink, TextAlignmentOptions.Center);
        voteLabel.textWrappingMode = TextWrappingModes.NoWrap;
        voteLabel.text = "Approved 3–0";
        var noInstruments = Empty(instPage, "NoInstruments", "No instruments placed yet.");

        // Page 2: action cards
        var cardsPage = NewRect("Cards", content);
        Column(cardsPage, 12, new RectOffset(0, 0, 0, 0));
        var card = NewRect("CardTemplate", cardsPage);
        Round(card, CardCream, 16);
        var cardOutline = card.gameObject.AddComponent<Outline>();
        cardOutline.effectColor = Orange;
        cardOutline.effectDistance = new Vector2(2.5f, -2.5f);
        Column(card, 6, new RectOffset(16, 16, 14, 14));
        var top = NewRect("Top", card);
        Fixed(top, -1, 46);
        Row(top, 10);
        var cardHex = NewRect("Hex", top);
        Fixed(cardHex, 40, 46);
        Icon(cardHex, hex, Orange).preserveAspect = false;
        var cardNumber = Txt(Stretch(NewRect("Number", cardHex)), bold, 15, Color.white, TextAlignmentOptions.Center);
        cardNumber.textWrappingMode = TextWrappingModes.NoWrap;
        cardNumber.text = "03";
        NewRect("Spacer", top).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        var kind = Txt(NewRect("Kind", top), bold, 13, Orange, TextAlignmentOptions.MidlineRight);
        kind.characterSpacing = 8;
        kind.textWrappingMode = TextWrappingModes.NoWrap;
        kind.text = "ACTION";
        Txt(NewRect("Title", card), bold, 20, Ink, TextAlignmentOptions.TopLeft).text = "Creating partnerships to enhance biodiversity";
        Txt(NewRect("Description", card), regular, 16, Body, TextAlignmentOptions.TopLeft).text = "What the action does.";
        var how = Txt(NewRect("How", card), regular, 15, Muted, TextAlignmentOptions.TopLeft);
        how.richText = true;
        how.text = "<b>How:</b> …";
        var uses = NewRect("Uses", card);
        Column(uses, 6, new RectOffset(0, 0, 6, 2));
        var usesLabel = Txt(NewRect("Label", uses), bold, 13, Muted, TextAlignmentOptions.TopLeft);
        usesLabel.characterSpacing = 8;
        usesLabel.text = "USES";
        var links = Flow(NewRect("Links", uses));
        var link = Chip(links, "LinkTemplate", Lilac, 30, new RectOffset(10, 12, 0, 0));
        var linkImg = link.GetComponent<Image>();
        linkImg.raycastTarget = true;
        link.gameObject.AddComponent<Button>().targetGraphic = linkImg;
        var linkDot = NewRect("Dot", link);
        Fixed(linkDot, 10, 10);
        Round(linkDot, Purple, 5);
        ChipLabel(link, bold, 14, Ink, "4 · Land Use Zoning");
        var cardLine = NewRect("Line", card);
        Fixed(cardLine, -1, 1.5f);
        cardLine.gameObject.AddComponent<Image>().color = new Color32(0xEF, 0xE6, 0xD2, 0xFF);
        var footer = NewRect("Footer", card);
        Fixed(footer, -1, 34);
        Row(footer, 8);
        var collabLabel = Txt(NewRect("Label", footer), bold, 13, Muted, TextAlignmentOptions.MidlineLeft);
        collabLabel.characterSpacing = 8;
        collabLabel.textWrappingMode = TextWrappingModes.NoWrap;
        collabLabel.text = "COLLABORATORS";
        var avatars = NewRect("Avatars", footer);
        Row(avatars, -6).childControlHeight = false;
        var people = Txt(NewRect("People", footer), regular, 15, Muted, TextAlignmentOptions.MidlineLeft);
        people.overflowMode = TextOverflowModes.Ellipsis;
        people.textWrappingMode = TextWrappingModes.NoWrap;
        people.text = "No one yet";
        var peopleSize = people.gameObject.AddComponent<LayoutElement>();
        peopleSize.flexibleWidth = 1;
        peopleSize.minWidth = 0;
        var avatarTemplate = NewRect("AvatarTemplate", cardsPage);
        Fixed(avatarTemplate, 30, 30);
        Round(avatarTemplate, PlayerCapsuleHook.PlayerColors[1], 15);
        var avatarRing = avatarTemplate.gameObject.AddComponent<Outline>();
        avatarRing.effectColor = CardCream;
        avatarRing.effectDistance = new Vector2(2, -2);
        var avatarNumber = Txt(Stretch(NewRect("Number", avatarTemplate)), bold, 14, Color.white, TextAlignmentOptions.Center);
        avatarNumber.text = "2";
        var noCards = Empty(cardsPage, "NoCards", "Hasn’t played any action cards yet.");

        // Page 3: collaborations
        var collabPage = NewRect("Collaborations", content);
        Column(collabPage, 8, new RectOffset(0, 0, 0, 0));
        SectionLabel(collabPage, "JOINED OTHERS’ ACTIONS", Muted);
        var joined = NewRect("Joined", collabPage);
        Column(joined, 8, new RectOffset(0, 0, 0, 0));
        var joinedRow = NewRect("JoinedTemplate", joined);
        Round(joinedRow, Lilac, 14);
        var joinedLayout = Row(joinedRow, 12);
        joinedLayout.padding = new RectOffset(12, 14, 10, 10);
        var joinedAvatar = NewRect("Avatar", joinedRow);
        Fixed(joinedAvatar, 34, 34);
        Round(joinedAvatar, PlayerCapsuleHook.PlayerColors[1], 17);
        Txt(Stretch(NewRect("Number", joinedAvatar)), bold, 15, Color.white, TextAlignmentOptions.Center).text = "2";
        var joinedTexts = NewRect("Texts", joinedRow);
        Column(joinedTexts, -2, new RectOffset(0, 0, 0, 0));
        joinedTexts.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        Txt(NewRect("Card", joinedTexts), bold, 17, Ink, TextAlignmentOptions.TopLeft).text = "04  Joining a network";
        Txt(NewRect("Owner", joinedTexts), regular, 14, Muted, TextAlignmentOptions.TopLeft).text = "Rui’s action";
        var joinedTag = NewRect("Tag", joinedRow);
        Fixed(joinedTag, 130, 28);
        Round(joinedTag, new Color32(0xDC, 0xEB, 0xFA, 0xFF), 14);
        var joinedTagLabel = Txt(Stretch(NewRect("Label", joinedTag)), bold, 14, TokenBlue, TextAlignmentOptions.Center);
        joinedTagLabel.textWrappingMode = TextWrappingModes.NoWrap;
        joinedTagLabel.text = "Collaborating";
        var noJoined = Empty(collabPage, "NoJoined", "Hasn’t joined anyone’s action yet.");
        var gap = NewRect("Gap", collabPage);
        Fixed(gap, -1, 10);
        SectionLabel(collabPage, "TOKENS", Muted);
        var tokens = NewRect("Tokens", collabPage);
        Fixed(tokens, -1, 30);
        Row(tokens, 6).childControlHeight = false;
        var token = NewRect("TokenTemplate", tokens);
        Fixed(token, 24, 24);
        Round(token, TokenBlue, 12);
        var tokensText = Txt(NewRect("Text", tokens), bold, 16, TokenBlue, TextAlignmentOptions.MidlineLeft);
        tokensText.textWrappingMode = TextWrappingModes.NoWrap;
        tokensText.margin = new Vector4(8, 0, 0, 0);
        tokensText.text = "5 of 5 tokens left";

        // ── Wire ──
        panel.root = root.gameObject;
        panel.rootGroup = group;
        panel.window = window;
        panel.leftFill = leftFill;
        panel.positionText = position;
        panel.prevButton = prev;
        panel.nextButton = next;
        panel.avatarIcon = avatarIcon;
        panel.badgeFill = badgeFill;
        panel.badgeNumber = badgeNumber;
        panel.nameText = nameText;
        panel.companyText = company;
        panel.statusDot = statusDotImg;
        panel.statusText = statusText;
        panel.statValues = statValues;
        panel.tabButtons = tabButtons;
        panel.tabLabels = tabLabels;
        panel.tabCountFills = tabCountFills;
        panel.tabCounts = tabCounts;
        panel.tabUnderlines = tabUnderlines;
        panel.tabPages = new[] { instPage.gameObject, cardsPage.gameObject, collabPage.gameObject };
        panel.scroll = sr;
        panel.closeButton = closeButton;
        panel.instrumentsContainer = instPage;
        panel.instrumentTemplate = instRow.gameObject;
        panel.noInstruments = noInstruments.gameObject;
        panel.cardsContainer = cardsPage;
        panel.cardTemplate = card.gameObject;
        panel.avatarTemplate = avatarTemplate.gameObject;
        panel.noCards = noCards;
        panel.joinedContainer = joined;
        panel.joinedTemplate = joinedRow.gameObject;
        panel.noJoined = noJoined.gameObject;
        panel.tokensContainer = tokens;
        panel.tokenTemplate = token.gameObject;
        panel.tokensText = tokensText;
        instRow.gameObject.SetActive(false);
        card.gameObject.SetActive(false);
        avatarTemplate.gameObject.SetActive(false);
        joinedRow.gameObject.SetActive(false);
        token.gameObject.SetActive(false);
        cardsPage.gameObject.SetActive(false);
        collabPage.gameObject.SetActive(false);
        root.gameObject.SetActive(false);
        EditorUtility.SetDirty(panel);

        UnityEventTools.AddPersistentListener(dimButton.onClick, new UnityAction(panel.Close));
    }

    // Round button with a chevron (angle 0 points right).
    private static Button CircleButton(RectTransform parent, string name, float x, float y, float size, Color fill, Color ink, float angle)
    {
        var rt = Place(NewRect(name, parent), x, y, size, size);
        var img = Round(rt, fill, size / 2f);
        img.raycastTarget = true;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        var arrow = NewRect("Arrow", rt);
        arrow.anchorMin = arrow.anchorMax = new Vector2(0.5f, 0.5f);
        arrow.sizeDelta = new Vector2(size * 0.42f, size * 0.42f);
        arrow.localRotation = Quaternion.Euler(0, 0, angle);
        var arrowImg = arrow.gameObject.AddComponent<Image>();
        arrowImg.sprite = ActionDrawerBuilder.GetOrCreateChevron();
        arrowImg.color = ink;
        arrowImg.raycastTarget = false;
        return button;
    }

    // Centred grey line for an empty list.
    private static TextMeshProUGUI Empty(RectTransform parent, string name, string text)
    {
        var label = Txt(NewRect(name, parent), regular, 18, new Color32(0x9A, 0x92, 0xAC, 0xFF), TextAlignmentOptions.Center);
        label.margin = new Vector4(0, 24, 0, 24);
        label.text = text;
        return label;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    // The avatar's round background; it also clips the person's shoulders to the circle.
    // The number badge goes on the avatar itself, outside the clipped circle.
    internal static Image Circle(RectTransform avatar, Color color, out RectTransform circle)
    {
        circle = Stretch(NewRect("Circle", avatar));
        var img = Round(circle, color, avatar.sizeDelta.x / 2f);
        circle.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        return img;
    }

    internal static Image Icon(RectTransform rt, Sprite sprite, Color color)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // Small circle with the player number, on the avatar's bottom-right edge. Its outline matches the avatar.
    private static RectTransform NumberBadge(RectTransform avatar, float size, Color fill, Color text, float fontSize, out TextMeshProUGUI number)
    {
        var dot = NewRect("Number", avatar);
        dot.anchorMin = dot.anchorMax = dot.pivot = new Vector2(1, 0);
        dot.sizeDelta = new Vector2(size, size);
        dot.anchoredPosition = new Vector2(size * 0.22f, -size * 0.12f);
        Round(dot, fill, size / 2f);
        var ring = dot.gameObject.AddComponent<Outline>();
        ring.effectColor = text;
        ring.effectDistance = new Vector2(1.5f, -1.5f);
        number = Txt(Stretch(NewRect("Text", dot)), bold, fontSize, text, TextAlignmentOptions.Center);
        number.textWrappingMode = TextWrappingModes.NoWrap;
        number.text = "1";
        return dot;
    }

    // White head-and-shoulders silhouette, made once into Assets/Images/Players/Person.png.
    internal static Sprite GetOrCreatePerson()
    {
        const string folder = "Assets/Images/Players/";
        const string path = folder + "Person.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing) return existing;

        System.IO.Directory.CreateDirectory(folder);
        const int w = 256, h = 256;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var head = new Vector2(128f, 170f);
        const float headR = 56f;
        var body = new Vector2(128f, 0f);       // shoulders: top half of an ellipse sitting on the bottom edge
        const float bodyRX = 108f, bodyRY = 100f;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            var p = new Vector2(x + 0.5f, y + 0.5f);
            float aHead = Mathf.Clamp01(headR - Vector2.Distance(p, head) + 0.5f);
            // Distance to the ellipse edge, roughly in pixels, for a soft edge.
            var q = new Vector2((p.x - body.x) / bodyRX, (p.y - body.y) / bodyRY);
            float aBody = Mathf.Clamp01((1f - q.magnitude) * Mathf.Min(bodyRX, bodyRY) + 0.5f);
            // A thin gap between head and shoulders reads better at small sizes.
            float gap = Mathf.Clamp01(Vector2.Distance(p, head) - (headR + 10f) + 0.5f);
            float a = Mathf.Max(aHead, aBody * gap);
            tex.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        tex.Apply();
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true; // drawn small on the capsules
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static VerticalLayoutGroup Column(RectTransform rt, float spacing, RectOffset padding)
    {
        var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = padding;
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return layout;
    }

    private static HorizontalLayoutGroup Row(RectTransform rt, float spacing)
    {
        var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        return layout;
    }

    // Fixed preferred size for a layout child (-1 leaves that axis to the layout).
    private static void Fixed(RectTransform rt, float w, float h)
    {
        var size = rt.GetComponent<LayoutElement>();
        if (!size) size = rt.gameObject.AddComponent<LayoutElement>();
        if (w >= 0) size.preferredWidth = size.minWidth = w;
        if (h >= 0) size.preferredHeight = size.minHeight = h;
        rt.sizeDelta = new Vector2(w >= 0 ? w : rt.sizeDelta.x, h >= 0 ? h : rt.sizeDelta.y);
    }

    // Rounded pill that sizes itself to its children (a dot or hexagon, then a label).
    private static RectTransform Chip(Component parent, string name, Color fill, float height, RectOffset padding)
    {
        var chip = NewRect(name, parent.transform);
        Round(chip, fill, height / 2f);
        var layout = Row(chip, 8);
        layout.padding = padding;
        layout.childControlHeight = false;
        Fixed(chip, -1, height);
        return chip;
    }

    private static TextMeshProUGUI ChipLabel(RectTransform chip, TMP_FontAsset font, float size, Color color, string text)
    {
        var label = Txt(NewRect("Label", chip), font, size, color, TextAlignmentOptions.MidlineLeft);
        label.rectTransform.sizeDelta = new Vector2(100, size * 1.4f);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.richText = true;
        label.text = text;
        return label;
    }

    private static TextMeshProUGUI Fact(HorizontalLayoutGroup row, string label)
    {
        var tile = NewRect("Fact", row.transform);
        Round(tile, Lilac, 16);
        var value = Txt(Place(NewRect("Value", tile), 16, 10, 150, 40), bold, 34, Purple, TextAlignmentOptions.TopLeft);
        value.textWrappingMode = TextWrappingModes.NoWrap;
        value.text = "0";
        var text = Txt(Place(NewRect("Label", tile), 16, 54, 160, 24), regular, 16, Muted, TextAlignmentOptions.TopLeft);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.text = label;
        return value;
    }

    private static void SectionLabel(RectTransform body, string text, Color color)
    {
        var label = Txt(NewRect("Section", body), bold, 15, color, TextAlignmentOptions.BottomLeft);
        label.characterSpacing = 6;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.text = text;
        Fixed(label.rectTransform, -1, 26);
    }

    private static RectTransform Flow(RectTransform rt)
    {
        var flow = rt.gameObject.AddComponent<FlowLayoutGroup>();
        flow.spacingX = flow.spacingY = 8;
        return rt;
    }

    private static GameObject Note(RectTransform body, string name, string text)
    {
        var note = Txt(NewRect(name, body), regular, 17, Muted, TextAlignmentOptions.MidlineLeft);
        note.fontStyle = FontStyles.Italic;
        note.text = text;
        Fixed(note.rectTransform, -1, 28);
        return note.gameObject;
    }
}
