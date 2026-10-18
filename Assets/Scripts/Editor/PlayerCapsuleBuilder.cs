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

    private const float W = 640f;     // details window width
    private const float HeaderH = 150f;

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
        dim.color = new Color(0.08f, 0.055f, 0.16f, 0.45f);
        var dimButton = dim.gameObject.AddComponent<Button>();
        dimButton.transition = Selectable.Transition.None;

        // The window grows with its content.
        var window = NewRect("Window", root);
        window.anchorMin = window.anchorMax = window.pivot = new Vector2(0.5f, 0.5f);
        window.sizeDelta = new Vector2(W, 400);
        var windowImg = Round(window, Color.white, 28);
        windowImg.raycastTarget = true; // clicks on the window don't close it
        var windowShadow = window.gameObject.AddComponent<Shadow>();
        windowShadow.effectColor = new Color(0.04f, 0.02f, 0.1f, 0.3f);
        windowShadow.effectDistance = new Vector2(0, -12);
        window.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        var windowLayout = Column(window, 0, new RectOffset(0, 0, 0, 0));
        windowLayout.childForceExpandWidth = true;
        window.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Header in the player's colour ──
        var header = NewRect("Header", window);
        Fixed(header, -1, HeaderH);
        var headerFill = header.gameObject.AddComponent<Image>();
        headerFill.color = PlayerCapsuleHook.PlayerColors[0];
        headerFill.raycastTarget = false;
        var shade = Stretch(NewRect("Shade", header)).gameObject.AddComponent<Image>();
        shade.color = new Color(0, 0, 0, 0.12f);
        shade.raycastTarget = false;
        var shadeRT = shade.rectTransform;
        shadeRT.anchorMax = new Vector2(1, 0.5f); // a darker lower half gives the header some depth
        var cascade = Stretch(NewRect("HexCascade", header)).gameObject.AddComponent<CardCascade>();
        cascade.cardSprite = hex;
        cascade.pixelsPerUnitMultiplier = 1f;
        cascade.cardSize = new Vector2(26, 30);
        cascade.color = new Color(1f, 1f, 1f, 0.16f);
        header.gameObject.AddComponent<RectMask2D>();

        // Profile picture: white circle, person in the player's colour, number in a dark corner badge.
        var bigBadge = NewRect("Avatar", header);
        bigBadge.anchorMin = bigBadge.anchorMax = bigBadge.pivot = new Vector2(0, 0.5f);
        bigBadge.sizeDelta = new Vector2(92, 92);
        bigBadge.anchoredPosition = new Vector2(30, 0);
        Circle(bigBadge, Color.white, out var bigCircle);
        var bigPerson = Stretch(NewRect("Person", bigCircle));
        bigPerson.offsetMin = new Vector2(15, 0);
        bigPerson.offsetMax = new Vector2(-15, -14);
        var avatarIcon = Icon(bigPerson, GetOrCreatePerson(), PlayerCapsuleHook.PlayerColors[0]);
        NumberBadge(bigBadge, 34, Ink, Color.white, 19, out var badgeNumber);

        const float textX = 140f, textW = W - textX - 80f;
        var eyebrow = Txt(Place(NewRect("Eyebrow", header), textX, 26, textW, 22), bold, 16, new Color(1, 1, 1, 0.85f), TextAlignmentOptions.TopLeft);
        eyebrow.characterSpacing = 6;
        eyebrow.text = "PLAYER 1";
        var nameText = Txt(Place(NewRect("Name", header), textX, 48, textW, 48), bold, 38, Color.white, TextAlignmentOptions.TopLeft);
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 26;
        nameText.fontSizeMax = 38;
        nameText.text = "Player name";
        var company = Txt(Place(NewRect("Company", header), textX, 98, textW, 30), regular, 21, Color.white, TextAlignmentOptions.TopLeft);
        company.textWrappingMode = TextWrappingModes.NoWrap;
        company.overflowMode = TextOverflowModes.Ellipsis;
        company.text = "Organisation";

        var closeRT = NewRect("Close", header);
        closeRT.anchorMin = closeRT.anchorMax = closeRT.pivot = new Vector2(1, 1);
        closeRT.sizeDelta = new Vector2(42, 42);
        closeRT.anchoredPosition = new Vector2(-18, -18);
        var closeImg = Round(closeRT, new Color(1, 1, 1, 0.25f), 21);
        closeImg.raycastTarget = true;
        var closeButton = closeRT.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImg;
        foreach (var angle in new[] { 45f, -45f })
        {
            var bar = NewRect("Bar", closeRT);
            bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0.5f, 0.5f);
            bar.sizeDelta = new Vector2(18, 3.5f);
            bar.localRotation = Quaternion.Euler(0, 0, angle);
            Round(bar, Color.white, 1.75f);
        }

        // ── Body ──
        var body = NewRect("Body", window);
        Column(body, 12, new RectOffset(26, 26, 20, 26));

        // Turn tag
        var turnRow = Row(NewRect("TurnRow", body), 0);
        var turnTag = Chip(turnRow, "TurnTag", PinkLight, 30, new RectOffset(12, 16, 0, 0));
        var turnDot = NewRect("Dot", turnTag);
        Round(turnDot, Pink, 5);
        Fixed(turnDot, 10, 10);
        ChipLabel(turnTag, bold, 17, PinkDark, "Placing an instrument now");
        turnRow.gameObject.SetActive(false);

        // Three numbers
        var facts = Row(NewRect("Facts", body), 12);
        facts.childForceExpandWidth = true;
        facts.childControlWidth = true;
        facts.childForceExpandHeight = true;
        Fixed((RectTransform)facts.transform, -1, 88);
        var tokensValue = Fact(facts, "Collaboration tokens");
        var instrumentsValue = Fact(facts, "Instruments placed");
        var cardsValue = Fact(facts, "Action cards played");

        // Instruments placed
        SectionLabel(body, "INSTRUMENTS PLACED", PurpleMid);
        var instruments = Flow(NewRect("Instruments", body));
        var instrumentTemplate = Chip(instruments, "InstrumentTemplate", Lilac, 40, new RectOffset(5, 16, 0, 0));
        var chipHex = NewRect("Hex", instrumentTemplate);
        Fixed(chipHex, 28, 32);
        var chipHexImg = chipHex.gameObject.AddComponent<Image>();
        chipHexImg.sprite = hex;
        chipHexImg.preserveAspect = true;
        chipHexImg.color = Purple;
        chipHexImg.raycastTarget = false;
        var chipNumber = Txt(Stretch(NewRect("Number", chipHex)), bold, 13, Color.white, TextAlignmentOptions.Center);
        chipNumber.textWrappingMode = TextWrappingModes.NoWrap;
        chipNumber.text = "0";
        ChipLabel(instrumentTemplate, medium, 17, Ink, "Instrument");
        var noInstruments = Note(body, "NoInstruments", "None yet");

        // Action cards played
        SectionLabel(body, "ACTION CARDS PLAYED", Orange);
        var cards = Flow(NewRect("Cards", body));
        var cardTemplate = Chip(cards, "CardTemplate", Cream, 40, new RectOffset(14, 16, 0, 0));
        var cardOutline = cardTemplate.gameObject.AddComponent<Outline>();
        cardOutline.effectColor = Orange;
        cardOutline.effectDistance = new Vector2(1.5f, -1.5f);
        ChipLabel(cardTemplate, medium, 17, Orange, "03  Card name");
        var noCards = Note(body, "NoCards", "None yet");

        // Open their played cards
        var buttonRow = Row(NewRect("ButtonRow", body), 0);
        buttonRow.padding = new RectOffset(0, 0, 6, 0);
        var showRT = NewRect("ShowCards", buttonRow.transform);
        Fixed(showRT, 260, 52);
        var showImg = Round(showRT, Orange, 26);
        showImg.raycastTarget = true;
        var showButton = showRT.gameObject.AddComponent<Button>();
        showButton.targetGraphic = showImg;
        var showLabel = Txt(Stretch(NewRect("Label", showRT)), bold, 20, Color.white, TextAlignmentOptions.Center);
        showLabel.textWrappingMode = TextWrappingModes.NoWrap;
        showLabel.text = "See played cards";

        // ── Wire ──
        panel.root = root.gameObject;
        panel.rootGroup = group;
        panel.window = window;
        panel.headerFill = headerFill;
        panel.badgeNumber = badgeNumber;
        panel.avatarIcon = avatarIcon;
        panel.eyebrow = eyebrow;
        panel.nameText = nameText;
        panel.companyText = company;
        panel.turnTag = turnRow.gameObject;
        panel.tokensValue = tokensValue;
        panel.instrumentsValue = instrumentsValue;
        panel.cardsValue = cardsValue;
        panel.instrumentsContainer = instruments;
        panel.instrumentTemplate = instrumentTemplate.gameObject;
        panel.noInstruments = noInstruments;
        panel.cardsContainer = cards;
        panel.cardTemplate = cardTemplate.gameObject;
        panel.noCards = noCards;
        panel.showCardsButton = showRT.gameObject;
        instrumentTemplate.gameObject.SetActive(false);
        cardTemplate.gameObject.SetActive(false);
        root.gameObject.SetActive(false);
        EditorUtility.SetDirty(panel);

        UnityEventTools.AddPersistentListener(dimButton.onClick, new UnityAction(panel.Close));
        UnityEventTools.AddPersistentListener(closeButton.onClick, new UnityAction(panel.Close));
        UnityEventTools.AddPersistentListener(showButton.onClick, new UnityAction(panel.ShowPlayedCards));
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
