using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static ActionCardBuilder;

// Builds the instruments round as a purple side tray + a hexagon stack on the right edge,
// inside the MainGame scene's instrument spawner area. The spawned pieces' container ("Content",
// with the InstrumentSpawner and its PhotonView) is moved into the tray. Safe to run again.
// The old "InstrumentSpawner" panel and "TEXT_Player Turn" stay in the scene, switched off.
public static class InstrumentTrayBuilder
{
    private const string ScenePath = "Assets/Scenes/MainGame.unity";

    private const float TrayWidth = 640f;
    private const float VerticalInset = 96f;   // keeps the player capsules (top and bottom) uncovered
    private const float RightGap = 76f;        // leaves the Game Log tab on the screen edge uncovered
    private const float ChipsTop = 102f;
    private const float ChipsHeight = 2 * 34 + 6; // two rows of chips
    private const float PiecesTop = ChipsTop + ChipsHeight + 10;
    // Tray pieces keep the piece's own 300:346 proportions, 3 per row.
    private const float PieceW = 180f, PieceH = 208f;

    private static readonly Color GradientTop = new Color32(0x9F, 0x86, 0xDC, 0xFF);
    private static readonly Color GradientMid = new Color32(0xC9, 0xB9, 0xEE, 0xFF);
    private static readonly Color GradientBottom = new Color32(0xF1, 0xEC, 0xFB, 0xFF);
    private static readonly Color Purple = new Color32(0x46, 0x32, 0x7F, 0xFF);
    private static readonly Color PurpleMid = new Color32(0x6A, 0x55, 0xA8, 0xFF);
    private static readonly Color Lilac = new Color32(0xED, 0xE6, 0xFA, 0xFF);
    private static readonly Color Pink = new Color32(0xFE, 0x46, 0xA5, 0xFF);
    private static readonly Color Ink = new Color32(0x24, 0x1B, 0x3A, 0xFF);
    private static readonly Color Muted = new Color32(0x5E, 0x55, 0x75, 0xFF);

    [MenuItem("BioValue/Build Instrument Tray")]
    public static void Build()
    {
        if (!LoadArt())
        {
            EditorUtility.DisplayDialog("Build Instrument Tray",
                "Missing art or font. Run BioValue/Build Instrument Piece and BioValue/Build Action Card first.", "OK");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        ObjectsManager objects = null;
        foreach (var o in Object.FindObjectsByType<ObjectsManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (o.gameObject.scene == scene) { objects = o; break; }
        var areaGO = objects ? new SerializedObject(objects).FindProperty("InstrumentSpawnerArea").objectReferenceValue as GameObject : null;
        InstrumentSpawner spawner = null;
        foreach (var s in Object.FindObjectsByType<InstrumentSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (s.gameObject.scene == scene) { spawner = s; break; }
        if (!areaGO || !spawner)
        {
            EditorUtility.DisplayDialog("Build Instrument Tray", "No instrument spawner area or InstrumentSpawner found in MainGame.", "OK");
            return;
        }
        var area = (RectTransform)areaGO.transform;
        var content = (RectTransform)(spawner.parentOfInstruments ? spawner.parentOfInstruments.transform : spawner.transform);

        // Get the pieces' container out before removing a previous build (it may live inside it).
        content.SetParent(area, false);
        foreach (var name in new[] { "InstrumentTray", "InstrumentStack" })
        {
            Transform old;
            while ((old = area.Find(name)) != null) Object.DestroyImmediate(old.gameObject);
        }
        var oldPanel = area.Find("InstrumentSpawner");
        if (oldPanel) oldPanel.gameObject.SetActive(false);
        var oldTurnText = area.Find("TEXT_Player Turn");
        if (oldTurnText) oldTurnText.gameObject.SetActive(false);

        var view = area.GetComponent<InstrumentTrayView>() ?? area.gameObject.AddComponent<InstrumentTrayView>();

        // ── Tray ──
        var drawer = NewRect("InstrumentTray", area);
        drawer.anchorMin = new Vector2(1, 0);
        drawer.anchorMax = new Vector2(1, 1);
        drawer.pivot = new Vector2(1, 0.5f);
        drawer.sizeDelta = new Vector2(TrayWidth, -2 * VerticalInset);
        drawer.anchoredPosition = new Vector2(-RightGap, 0);
        var group = drawer.gameObject.AddComponent<CanvasGroup>();
        Round(drawer, new Color(1, 1, 1, 0), 24).raycastTarget = true; // clicks on the tray don't reach the board

        // Look: rounded panel (also the mask), purple gradient, white hexagons cascading behind the pieces.
        var shadowRT = Stretch(NewRect("Shadow", drawer));
        shadowRT.offsetMin = shadowRT.offsetMax = new Vector2(-6, -8);
        Round(shadowRT, new Color(0.14f, 0.1f, 0.24f, 0.3f), 24);
        var back = Stretch(NewRect("Background", drawer));
        Round(back, Color.white, 24);
        back.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        var gradient = Stretch(NewRect("Gradient", back)).gameObject.AddComponent<RawImage>();
        gradient.texture = GetOrCreateGradient();
        gradient.raycastTarget = false;
        var cascade = Stretch(NewRect("HexCascade", back)).gameObject.AddComponent<CardCascade>();
        cascade.cardSprite = hex;
        cascade.pixelsPerUnitMultiplier = 1f;
        cascade.cardSize = new Vector2(30, 34);
        cascade.color = new Color(1f, 1f, 1f, 0.3f);

        // Turn banner
        var banner = TopRow(NewRect("TurnBanner", back), 16, 16, 16, 74);
        var bannerFill = Round(banner, Purple, 18);
        var dot = NewRect("Dot", banner);
        dot.anchorMin = dot.anchorMax = dot.pivot = new Vector2(0, 0.5f);
        dot.sizeDelta = new Vector2(14, 14);
        dot.anchoredPosition = new Vector2(18, 0);
        Round(dot, Pink, 7);
        var bannerTitle = Txt(TopRow(NewRect("Title", banner), 44, 10, 150, 30), bold, 24, Color.white, TextAlignmentOptions.TopLeft);
        bannerTitle.textWrappingMode = TextWrappingModes.NoWrap;
        bannerTitle.text = "Your turn";
        var bannerSub = Txt(TopRow(NewRect("Subtitle", banner), 44, 42, 150, 22), regular, 16, Color.white, TextAlignmentOptions.TopLeft);
        bannerSub.textWrappingMode = TextWrappingModes.NoWrap;
        bannerSub.text = "Place one instrument on the board";
        var left = Txt(NewRect("Left", banner), bold, 16, Color.white, TextAlignmentOptions.MidlineRight);
        left.rectTransform.anchorMin = left.rectTransform.anchorMax = left.rectTransform.pivot = new Vector2(1, 0.5f);
        left.rectTransform.sizeDelta = new Vector2(130, 30);
        left.rectTransform.anchoredPosition = new Vector2(-18, 0);
        left.text = "12 of 12 left";

        // Category chips: wrap onto a second row when they don't fit on one.
        var chipItems = TopRow(NewRect("Chips", back), 16, ChipsTop, 16, ChipsHeight);
        var chipsLayout = chipItems.gameObject.AddComponent<FlowLayoutGroup>();
        chipsLayout.spacingX = 8;
        chipsLayout.spacingY = 6;
        var chipTemplate = BuildChip(chipItems);

        // Pieces (scrolls); the spawner's container goes in here.
        var viewport = BuildPiecesScroll(back, content);

        // Handle on the left border: closes the tray (the hexagon stack brings it back).
        var handle = NewRect("CloseHandle", drawer);
        handle.anchorMin = handle.anchorMax = new Vector2(0, 0.5f);
        handle.pivot = new Vector2(1, 0.5f);
        handle.sizeDelta = new Vector2(44, 104);
        handle.anchoredPosition = new Vector2(10, 0);
        var handleImg = Round(handle, GradientMid, 14);
        handleImg.raycastTarget = true;
        var handleShadow = handle.gameObject.AddComponent<Shadow>();
        handleShadow.effectColor = new Color(0.14f, 0.1f, 0.24f, 0.25f);
        handleShadow.effectDistance = new Vector2(-4, -4);
        var handleButton = handle.gameObject.AddComponent<Button>();
        handleButton.targetGraphic = handleImg;
        var arrow = NewRect("Arrow", handle);
        arrow.anchorMin = arrow.anchorMax = new Vector2(0.5f, 0.5f);
        arrow.sizeDelta = new Vector2(22, 22);
        arrow.anchoredPosition = new Vector2(-4, 0);
        var arrowImg = arrow.gameObject.AddComponent<Image>();
        arrowImg.sprite = ActionDrawerBuilder.GetOrCreateChevron();
        arrowImg.color = Purple;
        arrowImg.raycastTarget = false;

        // Back to front: handle, shadow, panel, (later children are the panel's content)
        back.SetAsFirstSibling();
        shadowRT.SetAsFirstSibling();
        handle.SetAsFirstSibling();

        // ── Hexagon stack on the screen edge ──
        var stack = BuildStack(area, out var stackCount, out var turnTag);

        // ── Wire ──
        view.drawer = drawer;
        view.drawerGroup = group;
        view.stack = stack;
        view.stackTurnTag = turnTag;
        view.bannerFill = bannerFill;
        view.turnDot = dot.gameObject;
        view.bannerTitle = bannerTitle;
        view.bannerSubtitle = bannerSub;
        view.leftText = left;
        view.chipContainer = chipItems;
        view.chipTemplate = chipTemplate;
        view.content = content;
        view.cellHighlightSprite = hex;
        stack.countText = stackCount;
        stack.angles = new[] { 14f, 6f, 0f };
        stack.gameObject.SetActive(false); // the tray starts open
        EditorUtility.SetDirty(view);
        EditorUtility.SetDirty(stack);

        var fit = content.GetComponent<FitGridToViewport>();
        if (fit)
        {
            var fitSO = new SerializedObject(fit);
            fitSO.FindProperty("viewport").objectReferenceValue = viewport;
            fitSO.ApplyModifiedPropertiesWithoutUndo();
        }

        UnityEventTools.AddPersistentListener(stack.GetComponent<Button>().onClick, new UnityAction(view.Toggle));
        UnityEventTools.AddPersistentListener(handleButton.onClick, new UnityAction(view.Toggle));

        // ── Details window (long press), on the root canvas so it also works in the actions round ──
        var canvas = area.GetComponentInParent<Canvas>(true).rootCanvas.transform;
        Transform oldDetails;
        while ((oldDetails = canvas.Find("InstrumentDetailsPanel")) != null) Object.DestroyImmediate(oldDetails.gameObject);
        InstrumentDetailsBuilder.Build(canvas, view);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[InstrumentTrayBuilder] Instrument tray and hexagon stack built in MainGame (scene saved).");
    }

    // Purple at the top fading to lilac-white, made once into Assets/Images/InstrumentPiece/TrayGradient.png.
    internal static Texture2D GetOrCreateGradient()
    {
        const string path = "Assets/Images/InstrumentPiece/TrayGradient.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing) return existing;

        const int w = 4, h = 256;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            float t = 1f - y / (h - 1f); // 0 at the top
            var c = t < 0.5f
                ? Color.Lerp(GradientTop, GradientMid, Mathf.SmoothStep(0f, 1f, t * 2f))
                : Color.Lerp(GradientMid, GradientBottom, Mathf.SmoothStep(0f, 1f, t * 2f - 1f));
            for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply();
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // One filter chip: [● Planning 3]. The view clones it per category.
    private static GameObject BuildChip(RectTransform parent)
    {
        var chip = NewRect("ChipTemplate", parent);
        chip.sizeDelta = new Vector2(100, 34);
        var fill = Round(chip, new Color(1, 1, 1, 0.65f), 17);
        fill.raycastTarget = true;
        var button = chip.gameObject.AddComponent<Button>();
        button.targetGraphic = fill;
        var layout = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 14, 0, 0);
        layout.spacing = 6;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        var dot = NewRect("Dot", chip);
        dot.sizeDelta = new Vector2(11, 11);
        Round(dot, Purple, 5.5f);
        var dotSize = dot.gameObject.AddComponent<LayoutElement>();
        dotSize.preferredWidth = dotSize.minWidth = 11;

        var label = Txt(NewRect("Label", chip), bold, 15, Ink, TextAlignmentOptions.MidlineLeft);
        label.rectTransform.sizeDelta = new Vector2(80, 34);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.text = "All 12";
        return chip.gameObject;
    }

    // Vertical ScrollRect between the chips and the details card; returns its viewport.
    private static RectTransform BuildPiecesScroll(RectTransform parent, RectTransform content)
    {
        var scroll = NewRect("Pieces", parent);
        scroll.anchorMin = Vector2.zero;
        scroll.anchorMax = Vector2.one;
        scroll.offsetMin = new Vector2(0, 16);
        scroll.offsetMax = new Vector2(0, -PiecesTop);

        var viewport = Stretch(NewRect("Viewport", scroll));
        viewport.offsetMin = new Vector2(12, 0);
        viewport.offsetMax = new Vector2(-22, 0);
        viewport.gameObject.AddComponent<RectMask2D>();
        var hit = viewport.gameObject.AddComponent<Image>();
        hit.color = new Color(1, 1, 1, 0); // lets the wheel/drag scroll between pieces

        content.SetParent(viewport, false);
        content.localScale = Vector3.one;
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        var grid = content.GetComponent<GridLayoutGroup>();
        if (grid)
        {
            grid.cellSize = new Vector2(PieceW, PieceH);
            grid.spacing = new Vector2(10, 10);
            grid.padding = new RectOffset(4, 4, 6, 16);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;
            EditorUtility.SetDirty(grid);
        }
        content.sizeDelta = new Vector2(3 * PieceW + 2 * 10 + 8, content.sizeDelta.y);

        var bar = NewRect("Scrollbar", scroll);
        bar.anchorMin = new Vector2(1, 0);
        bar.anchorMax = new Vector2(1, 1);
        bar.pivot = new Vector2(1, 0.5f);
        bar.sizeDelta = new Vector2(8, -16);
        bar.anchoredPosition = new Vector2(-10, 0);
        Round(bar, new Color(1, 1, 1, 0.45f), 4).raycastTarget = true;
        var area = Stretch(NewRect("Sliding Area", bar));
        var handle = Stretch(NewRect("Handle", area));
        var handleImg = Round(handle, PurpleMid, 4);
        handleImg.raycastTarget = true;
        var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handleImg;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        var sr = scroll.gameObject.AddComponent<ScrollRect>();
        sr.content = content;
        sr.viewport = viewport;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 40;
        sr.verticalScrollbar = scrollbar;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        return viewport;
    }

    // Three hexagons peeking in from the right edge, an "Instruments N" pill and a pink "Your turn" tag.
    private static HandFan BuildStack(Transform area, out TMP_Text countText, out GameObject turnTag)
    {
        var root = NewRect("InstrumentStack", area);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(1, 0.5f);
        root.sizeDelta = new Vector2(230, 300);
        root.anchoredPosition = new Vector2(0, 190); // above the Game Log tab
        var hit = root.gameObject.AddComponent<Image>();
        hit.color = new Color(1, 1, 1, 0);
        var button = root.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        var fan = root.gameObject.AddComponent<HandFan>();

        var colors = new[] { Purple, PurpleMid, Lilac };
        var cards = new RectTransform[colors.Length];
        for (int i = 0; i < cards.Length; i++)
        {
            var card = NewRect("Hex " + (i + 1), root);
            card.anchorMin = card.anchorMax = Vector2.zero;
            card.pivot = new Vector2(0.5f, 0);
            card.sizeDelta = new Vector2(170, 196);
            card.anchoredPosition = new Vector2(176, 30);
            var img = card.gameObject.AddComponent<Image>();
            img.sprite = hex;
            img.preserveAspect = true;
            img.color = colors[i];
            img.raycastTarget = false;
            var sh = card.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.14f, 0.1f, 0.24f, 0.3f);
            sh.effectDistance = new Vector2(-3, -4);
            if (i == cards.Length - 1)
            {
                var mark = Txt(Stretch(NewRect("Mark", card)), bold, 64, Purple, TextAlignmentOptions.Center);
                mark.rectTransform.offsetMax = new Vector2(-24, 0); // the right part is off screen
                mark.text = "i";
            }
            cards[i] = card;
        }
        fan.cards = cards;

        // Pill: "Instruments  12"
        var pill = NewRect("Pill", root);
        pill.anchorMin = pill.anchorMax = pill.pivot = new Vector2(0, 1);
        pill.sizeDelta = new Vector2(176, 42);
        pill.anchoredPosition = new Vector2(0, -6);
        Round(pill, Color.white, 21);
        var pillShadow = pill.gameObject.AddComponent<Shadow>();
        pillShadow.effectColor = new Color(0, 0, 0, 0.25f);
        pillShadow.effectDistance = new Vector2(0, -3);
        var pillLabel = Txt(NewRect("Label", pill), bold, 17, Ink, TextAlignmentOptions.MidlineLeft);
        pillLabel.rectTransform.anchorMin = Vector2.zero;
        pillLabel.rectTransform.anchorMax = Vector2.one;
        pillLabel.rectTransform.offsetMin = new Vector2(16, 0);
        pillLabel.rectTransform.offsetMax = Vector2.zero;
        pillLabel.text = "Instruments";
        var bubble = NewRect("Count", pill);
        bubble.anchorMin = bubble.anchorMax = bubble.pivot = new Vector2(1, 0.5f);
        bubble.sizeDelta = new Vector2(42, 30);
        bubble.anchoredPosition = new Vector2(-6, 0);
        Round(bubble, Purple, 15);
        countText = Txt(Stretch(NewRect("Text", bubble)), bold, 15, Color.white, TextAlignmentOptions.Center);
        countText.text = "12";

        // Pink "Your turn" tag under the pill
        var tag = NewRect("TurnTag", root);
        tag.anchorMin = tag.anchorMax = tag.pivot = new Vector2(0, 1);
        tag.sizeDelta = new Vector2(112, 30);
        tag.anchoredPosition = new Vector2(10, -56);
        Round(tag, Pink, 15);
        Txt(Stretch(NewRect("Label", tag)), bold, 15, Color.white, TextAlignmentOptions.Center).text = "Your turn";
        tag.gameObject.SetActive(false);
        turnTag = tag.gameObject;
        return fan;
    }

    // Full-width row with left/right insets, positioned from the parent's top edge.
    private static RectTransform TopRow(RectTransform rt, float left, float top, float right, float height)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 1);
        rt.offsetMin = new Vector2(left, -top - height);
        rt.offsetMax = new Vector2(-right, -top);
        return rt;
    }
}
