using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static ActionCardBuilder;

// Builds the action deck as a light side drawer + a fan of card backs on the right edge,
// inside the MainGame scene's "ACTIONS DECK" area. Safe to run again (rebuilds both).
// The old full-screen panel and the "Actions" pile stay in the scene but are no longer used.
public static class ActionDrawerBuilder
{
    private const string ScenePath = "Assets/Scenes/MainGame.unity";

    private const float DrawerWidth = 744f;
    private const float HeaderHeight = 80f;
    private const float FooterHeight = 156f;

    // Drawer background: orange at the top fading to light orange at the bottom; the cards are cream.
    private static readonly Color GradientTop = new Color32(0xFF, 0xB2, 0x5C, 0xFF);
    private static readonly Color GradientBottom = new Color32(0xFF, 0xEC, 0xD2, 0xFF);
    private static readonly Color GradientMid = Color.Lerp(GradientTop, GradientBottom, 0.5f);
    private static readonly Color Ink = new Color32(0x2A, 0x21, 0x1A, 0xFF);
    private static readonly Color Muted = new Color32(0x6B, 0x5A, 0x48, 0xFF);
    private static readonly Color Brown = new Color32(0x8A, 0x4C, 0x07, 0xFF);
    private static readonly Color Line = new Color32(0xEA, 0xD9, 0xC2, 0xFF);
    private static readonly Color Orange = new Color32(0xB5, 0x65, 0x0A, 0xFF);
    private static readonly Color Pink = new Color32(0xFE, 0x46, 0xA5, 0xFF);
    private static readonly Color Warning = new Color32(0x8A, 0x3A, 0x0C, 0xFF);
    private static readonly Color AlertOrange = new Color32(0xE0, 0x74, 0x2E, 0xFF);

    [MenuItem("BioValue/Build Action Drawer")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Build Action Drawer", "Stop Play mode first: the builder edits the scene.", "OK");
            return;
        }
        if (!LoadArt())
        {
            EditorUtility.DisplayDialog("Build Action Drawer",
                "Missing art or font. Run BioValue/Build Instrument Piece and BioValue/Build Action Card first.", "OK");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        ActionDeck deck = null;
        foreach (var d in Object.FindObjectsByType<ActionDeck>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (d.gameObject.scene == scene) { deck = d; break; }
        if (!deck)
        {
            EditorUtility.DisplayDialog("Build Action Drawer", "No ActionDeck found in MainGame.", "OK");
            return;
        }
        var spawner = deck.GetComponent<ActionCardsSpawner>();
        var area = deck.transform.parent;

        foreach (var name in new[] { "ActionDrawer", "ActionHandFan" })
        {
            var old = area.Find(name);
            if (old) Object.DestroyImmediate(old.gameObject);
        }

        // Old "Actions" pile: keep the object (the deck scripts live on it), hide its look and click.
        var coverImage = deck.GetComponent<Image>();
        if (coverImage) coverImage.enabled = false;
        var coverButton = deck.GetComponent<Button>();
        if (coverButton) coverButton.enabled = false;
        foreach (Transform child in deck.transform) child.gameObject.SetActive(false);

        var view = area.GetComponent<ActionDrawerView>() ?? area.gameObject.AddComponent<ActionDrawerView>();

        // ── Drawer ──
        var drawer = NewRect("ActionDrawer", area);
        drawer.anchorMin = new Vector2(1, 0);
        drawer.anchorMax = new Vector2(1, 1);
        drawer.pivot = new Vector2(1, 0.5f);
        drawer.sizeDelta = new Vector2(DrawerWidth, -40);
        drawer.anchoredPosition = new Vector2(-20, 0);
        var bg = Round(drawer, new Color(1, 1, 1, 0), 24);
        bg.raycastTarget = true; // invisible, but clicks on the drawer don't reach the board

        // Look: rounded panel (also the mask) with an orange -> light orange gradient and
        // mini cards cascading diagonally behind the hand.
        var back = Stretch(NewRect("Background", drawer));
        Round(back, Color.white, 24);
        back.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        // Drop shadow as its own shape: on the mask it would let the mini cards show through it.
        var shadowRT = Stretch(NewRect("Shadow", drawer));
        shadowRT.offsetMin = new Vector2(-6, -8);
        shadowRT.offsetMax = new Vector2(-6, -8);
        Round(shadowRT, new Color(0.16f, 0.13f, 0.1f, 0.28f), 24);
        var gradient = Stretch(NewRect("Gradient", back)).gameObject.AddComponent<RawImage>();
        gradient.texture = GetOrCreateGradient();
        gradient.raycastTarget = false;
        var cascade = Stretch(NewRect("CardCascade", back)).gameObject.AddComponent<CardCascade>();
        cascade.cardSprite = round;
        cascade.pixelsPerUnitMultiplier = 24f / 6f; // 6px corners
        back.SetAsFirstSibling();
        shadowRT.SetAsFirstSibling(); // behind the panel

        // Header
        var header = TopBand(NewRect("Header", drawer), 0, HeaderHeight);
        var title = Txt(TopRow(NewRect("Title", header), 24, 24, 24, 36), bold, 28, Ink, TextAlignmentOptions.TopLeft);
        title.text = "Choose your action cards";

        Divider(header, false);

        // Footer: "Your play"
        var footer = NewRect("Footer", drawer);
        footer.anchorMin = new Vector2(0, 0);
        footer.anchorMax = new Vector2(1, 0);
        footer.pivot = new Vector2(0.5f, 0);
        footer.sizeDelta = new Vector2(0, FooterHeight);
        footer.anchoredPosition = Vector2.zero;
        Divider(footer, true);

        var yourPlay = NewRect("YourPlay", footer);
        yourPlay.anchorMin = yourPlay.anchorMax = yourPlay.pivot = new Vector2(0, 1);
        yourPlay.sizeDelta = new Vector2(110, 24);
        yourPlay.anchoredPosition = new Vector2(24, -14);
        Txt(yourPlay, bold, 19, Ink, TextAlignmentOptions.TopLeft).text = "Your play";
        var summary = Txt(TopRow(NewRect("Summary", footer), 138, 17, 24, 22), regular, 15, Muted, TextAlignmentOptions.TopLeft);
        summary.text = "Nothing selected yet";

        // Tray: one small card per selected action, scrolls sideways when there are many.
        var tray = TopRow(NewRect("Tray", footer), 24, 46, 24 + 200 + 16, 60);
        tray.gameObject.AddComponent<RectMask2D>();
        var trayHit = tray.gameObject.AddComponent<Image>();
        trayHit.color = new Color(1, 1, 1, 0);
        var trayItems = NewRect("Items", tray);
        trayItems.anchorMin = new Vector2(0, 0);
        trayItems.anchorMax = new Vector2(0, 1);
        trayItems.pivot = new Vector2(0, 0.5f);
        trayItems.sizeDelta = Vector2.zero;
        trayItems.anchoredPosition = Vector2.zero;
        var trayLayout = trayItems.gameObject.AddComponent<HorizontalLayoutGroup>();
        trayLayout.spacing = 8;
        trayLayout.padding = new RectOffset(2, 12, 0, 0);
        trayLayout.childAlignment = TextAnchor.MiddleLeft;
        trayLayout.childControlWidth = trayLayout.childControlHeight = false;
        trayLayout.childForceExpandWidth = trayLayout.childForceExpandHeight = false;
        var trayFit = trayItems.gameObject.AddComponent<ContentSizeFitter>();
        trayFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        var trayScroll = tray.gameObject.AddComponent<ScrollRect>();
        trayScroll.content = trayItems;
        trayScroll.viewport = tray;
        trayScroll.horizontal = true;
        trayScroll.vertical = false;
        trayScroll.movementType = ScrollRect.MovementType.Clamped;
        trayScroll.scrollSensitivity = 30;
        var trayItemTemplate = BuildTrayItem(trayItems);
        var trayEmpty = Txt(Stretch(NewRect("Empty", tray)), regular, 15, Muted, TextAlignmentOptions.MidlineLeft);
        trayEmpty.fontStyle = FontStyles.Italic;
        trayEmpty.text = "Select cards above to add them here";

        // First problem, one line under the tray.
        var reasonRow = TopRow(NewRect("Reason", footer), 24, 116, 24, 24);
        var reasonIcon = NewRect("Icon", reasonRow);
        reasonIcon.anchorMin = reasonIcon.anchorMax = reasonIcon.pivot = new Vector2(0, 0.5f);
        reasonIcon.sizeDelta = new Vector2(18, 18);
        reasonIcon.anchoredPosition = Vector2.zero;
        Round(reasonIcon, AlertOrange, 9);
        Txt(Stretch(NewRect("Mark", reasonIcon)), bold, 13, Color.white, TextAlignmentOptions.Center).text = "!";
        var reason = Txt(Stretch(NewRect("Text", reasonRow)), regular, 15, Warning, TextAlignmentOptions.MidlineLeft);
        reason.rectTransform.offsetMin = new Vector2(26, 0);
        reason.textWrappingMode = TextWrappingModes.NoWrap;
        reason.text = "O1 needs a description.";
        reasonRow.gameObject.SetActive(false);

        var play = NewRect("PlayButton", footer);
        play.anchorMin = play.anchorMax = play.pivot = new Vector2(1, 1);
        play.sizeDelta = new Vector2(200, 58);
        play.anchoredPosition = new Vector2(-24, -47);
        var playFill = Round(play, new Color32(0xE6, 0xDC, 0xCD, 0xFF), 14);
        playFill.raycastTarget = true;
        var playButton = play.gameObject.AddComponent<Button>();
        playButton.targetGraphic = playFill;
        var playColors = playButton.colors;
        playColors.disabledColor = Color.white; // the view colours the disabled state itself
        playButton.colors = playColors;
        var playLabel = Txt(Stretch(NewRect("Label", play)), bold, 20, new Color32(0x7A, 0x6A, 0x58, 0xFF), TextAlignmentOptions.Center);
        playLabel.text = "Play cards";

        // Handle on the left border: closes the drawer (the card fan brings it back).
        var handle = NewRect("CloseHandle", drawer);
        handle.anchorMin = handle.anchorMax = new Vector2(0, 0.5f);
        handle.pivot = new Vector2(1, 0.5f);
        handle.sizeDelta = new Vector2(44, 104);
        handle.anchoredPosition = new Vector2(10, 0);
        var handleImg = Round(handle, GradientMid, 14);
        handleImg.raycastTarget = true;
        var handleShadow = handle.gameObject.AddComponent<Shadow>();
        handleShadow.effectColor = new Color(0.16f, 0.13f, 0.1f, 0.22f);
        handleShadow.effectDistance = new Vector2(-4, -4);
        var handleButton = handle.gameObject.AddComponent<Button>();
        handleButton.targetGraphic = handleImg;
        var handleColors = handleButton.colors;
        handleColors.highlightedColor = new Color32(0xF1, 0xE6, 0xD6, 0xFF);
        handleButton.colors = handleColors;
        var arrow = NewRect("Arrow", handle);
        arrow.anchorMin = arrow.anchorMax = new Vector2(0.5f, 0.5f);
        arrow.sizeDelta = new Vector2(22, 22);
        arrow.anchoredPosition = new Vector2(-4, 0);
        var arrowImg = arrow.gameObject.AddComponent<Image>();
        arrowImg.sprite = GetOrCreateChevron();
        arrowImg.color = Ink;
        arrowImg.raycastTarget = false;
        handle.SetAsFirstSibling(); // behind the drawer's edge, so it looks attached

        // Card grid (scrolls)
        var content = BuildScroll(drawer, out var cardsScroll);

        // ── Card fan on the screen edge ──
        var fan = BuildFan(area, out var fanCount);
        AlignWithInstrumentStack((RectTransform)fan.transform, (RectTransform)area);
        KeepAboveGameLogTab((RectTransform)fan.transform, scene);

        // ── Wire ──
        view.drawer = drawer;
        view.fan = fan;
        view.title = title;
        view.cardsScroll = cardsScroll;
        view.summaryText = summary;
        view.trayContent = trayItems;
        view.trayItemTemplate = trayItemTemplate;
        view.trayEmpty = trayEmpty.gameObject;
        view.reasonRow = reasonRow.gameObject;
        view.reasonText = reason;
        view.playButton = playButton;
        view.playFill = playFill;
        view.playLabel = playLabel;
        fan.countText = fanCount;
        // Hidden until the actions round starts (ActionDeck.OnEnable brings the fan in).
        drawer.gameObject.SetActive(false);
        fan.gameObject.SetActive(false);
        EditorUtility.SetDirty(view);
        EditorUtility.SetDirty(fan);

        var deckSO = new SerializedObject(deck);
        deckSO.FindProperty("drawerView").objectReferenceValue = view;
        deckSO.ApplyModifiedPropertiesWithoutUndo();

        if (spawner)
        {
            var spSO = new SerializedObject(spawner);
            spSO.FindProperty("panelParent").objectReferenceValue = content;
            spSO.FindProperty("maxCardsPerRound").intValue = 0; // no limit for now
            spSO.ApplyModifiedPropertiesWithoutUndo();
        }

        UnityEventTools.AddPersistentListener(fan.GetComponent<Button>().onClick, new UnityAction(deck.ToggleMenu));
        UnityEventTools.AddPersistentListener(handleButton.onClick, new UnityAction(deck.CloseMenu));
        UnityEventTools.AddPersistentListener(playButton.onClick, new UnityAction(deck.PlaySelectedCardsButton));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ActionDrawerBuilder] Action drawer and card fan built in MainGame (scene saved).");
    }

    // ── GM: "Everyone's actions" ─────────────────────────────────────────────

    private const string ActionCardPrefabPath = "Assets/Resources/PhotonPrefabs/ActionCard.prefab";
    private const float GmHeaderHeight = 96f;
    private static readonly Color GmPink = new Color32(0xFE, 0x46, 0xA5, 0xFF);

    // Same look as the players' drawer, on the right of the screen: one section per player with the
    // cards they played. Built under the main canvas by "BioValue/Build Turn Ribbon and GM Panel".
    internal static GmActionsDrawer BuildGmDrawer(RectTransform root)
    {
        var holder = Stretch(NewRect("GmActionsDrawer", root));
        var view = holder.gameObject.AddComponent<GmActionsDrawer>();

        var drawer = NewRect("Drawer", holder);
        drawer.anchorMin = new Vector2(1, 0);
        drawer.anchorMax = new Vector2(1, 1);
        drawer.pivot = new Vector2(1, 0.5f);
        drawer.sizeDelta = new Vector2(DrawerWidth, -40);
        drawer.anchoredPosition = new Vector2(-20, 0);
        Round(drawer, new Color(1, 1, 1, 0), 24).raycastTarget = true;

        var back = Stretch(NewRect("Background", drawer));
        Round(back, Color.white, 24);
        back.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        var shadowRT = Stretch(NewRect("Shadow", drawer));
        shadowRT.offsetMin = new Vector2(-6, -8);
        shadowRT.offsetMax = new Vector2(-6, -8);
        Round(shadowRT, new Color(0.16f, 0.13f, 0.1f, 0.28f), 24);
        var gradient = Stretch(NewRect("Gradient", back)).gameObject.AddComponent<RawImage>();
        gradient.texture = GetOrCreateGradient();
        gradient.raycastTarget = false;
        var cascade = Stretch(NewRect("CardCascade", back)).gameObject.AddComponent<CardCascade>();
        cascade.cardSprite = round;
        cascade.pixelsPerUnitMultiplier = 24f / 6f;
        back.SetAsFirstSibling();
        shadowRT.SetAsFirstSibling();

        // Header: title, "ONLY YOU SEE THIS", how many have played.
        var header = TopBand(NewRect("Header", drawer), 0, GmHeaderHeight);
        var title = Txt(TopRow(NewRect("Title", header), 24, 18, 230, 36), bold, 28, Ink, TextAlignmentOptions.TopLeft);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.text = "Everyone’s actions";
        var tag = NewRect("OnlyYouTag", header);
        tag.anchorMin = tag.anchorMax = tag.pivot = new Vector2(1, 1);
        tag.anchoredPosition = new Vector2(-24, -22);
        tag.sizeDelta = new Vector2(196, 30);
        Round(tag, GmPink, 15);
        var tagLabel = Txt(Stretch(NewRect("Label", tag)), bold, 14, Color.white, TextAlignmentOptions.Center);
        tagLabel.characterSpacing = 4;
        tagLabel.textWrappingMode = TextWrappingModes.NoWrap;
        tagLabel.text = "ONLY YOU SEE THIS";
        var subtitle = Txt(TopRow(NewRect("Subtitle", header), 24, 58, 24, 24), regular, 16, Muted, TextAlignmentOptions.TopLeft);
        subtitle.text = "0 of 0 players have played their cards";
        Divider(header, false);

        // Close handle on the left border.
        var handle = NewRect("CloseHandle", drawer);
        handle.anchorMin = handle.anchorMax = new Vector2(0, 0.5f);
        handle.pivot = new Vector2(1, 0.5f);
        handle.sizeDelta = new Vector2(44, 104);
        handle.anchoredPosition = new Vector2(10, 0);
        var handleImg = Round(handle, GradientMid, 14);
        handleImg.raycastTarget = true;
        var handleShadow = handle.gameObject.AddComponent<Shadow>();
        handleShadow.effectColor = new Color(0.16f, 0.13f, 0.1f, 0.22f);
        handleShadow.effectDistance = new Vector2(-4, -4);
        var handleButton = handle.gameObject.AddComponent<Button>();
        handleButton.targetGraphic = handleImg;
        var arrow = NewRect("Arrow", handle);
        arrow.anchorMin = arrow.anchorMax = new Vector2(0.5f, 0.5f);
        arrow.sizeDelta = new Vector2(22, 22);
        arrow.anchoredPosition = new Vector2(-4, 0);
        var arrowImg = arrow.gameObject.AddComponent<Image>();
        arrowImg.sprite = GetOrCreateChevron();
        arrowImg.color = Ink;
        arrowImg.raycastTarget = false;
        handle.SetAsFirstSibling();

        // Scrolling list of player sections.
        var scroll = NewRect("Scroll", drawer);
        scroll.anchorMin = Vector2.zero;
        scroll.anchorMax = Vector2.one;
        scroll.offsetMin = new Vector2(0, 12);
        scroll.offsetMax = new Vector2(0, -GmHeaderHeight);
        var viewport = Stretch(NewRect("Viewport", scroll));
        viewport.offsetMax = new Vector2(-16, 0);
        viewport.gameObject.AddComponent<RectMask2D>();
        viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0);
        var sections = NewRect("Sections", viewport);
        sections.anchorMin = new Vector2(0, 1);
        sections.anchorMax = new Vector2(1, 1);
        sections.pivot = new Vector2(0.5f, 1);
        sections.sizeDelta = Vector2.zero;
        var list = sections.gameObject.AddComponent<VerticalLayoutGroup>();
        list.spacing = 4;
        list.padding = new RectOffset(0, 0, 8, 20);
        list.childControlWidth = list.childControlHeight = true;
        list.childForceExpandWidth = true;
        list.childForceExpandHeight = false;
        sections.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var bar = NewRect("Scrollbar", scroll);
        bar.anchorMin = new Vector2(1, 0);
        bar.anchorMax = new Vector2(1, 1);
        bar.pivot = new Vector2(1, 0.5f);
        bar.sizeDelta = new Vector2(8, -24);
        bar.anchoredPosition = new Vector2(-8, 0);
        Round(bar, Line, 4).raycastTarget = true;
        var barHandle = Stretch(NewRect("Handle", Stretch(NewRect("Sliding Area", bar))));
        var barHandleImg = Round(barHandle, new Color32(0xC9, 0xB7, 0x9F, 0xFF), 4);
        barHandleImg.raycastTarget = true;
        var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = barHandle;
        scrollbar.targetGraphic = barHandleImg;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        var sr = scroll.gameObject.AddComponent<ScrollRect>();
        sr.content = sections;
        sr.viewport = viewport;
        sr.horizontal = false;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 40;
        sr.verticalScrollbar = scrollbar;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        var section = BuildGmSection(sections);

        view.drawer = drawer;
        view.subtitle = subtitle;
        view.scroll = sr;
        view.sections = sections;
        view.sectionTemplate = section;
        view.actionCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ActionCardPrefabPath);
        if (!view.actionCardPrefab) Debug.LogWarning("[ActionDrawerBuilder] No action card prefab at " + ActionCardPrefabPath);
        UnityEventTools.AddPersistentListener(handleButton.onClick, new UnityAction(view.Close));
        drawer.gameObject.SetActive(false);
        EditorUtility.SetDirty(view);
        return view;
    }

    // One player: avatar + name + company + status, then their cards (2 columns, same size as the deck).
    private static GameObject BuildGmSection(RectTransform parent)
    {
        var section = NewRect("SectionTemplate", parent);
        var col = section.gameObject.AddComponent<VerticalLayoutGroup>();
        col.spacing = 12;
        col.padding = new RectOffset(20, 20, 14, 14);
        col.childControlWidth = col.childControlHeight = true;
        col.childForceExpandWidth = true;
        col.childForceExpandHeight = false;

        var header = NewRect("Header", section);
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
        var avatar = NewRect("Avatar", header);
        avatar.anchorMin = avatar.anchorMax = avatar.pivot = new Vector2(0, 0.5f);
        avatar.anchoredPosition = new Vector2(4, 0);
        avatar.sizeDelta = new Vector2(48, 48);
        Round(avatar, Orange, 24);
        Txt(Stretch(NewRect("Number", avatar)), bold, 20, Color.white, TextAlignmentOptions.Center).text = "1";
        var name = Txt(TopRow(NewRect("Name", header), 66, 2, 190, 30), bold, 22, Ink, TextAlignmentOptions.TopLeft);
        name.textWrappingMode = TextWrappingModes.NoWrap;
        name.overflowMode = TextOverflowModes.Ellipsis;
        name.text = "Player";
        var company = Txt(TopRow(NewRect("Company", header), 66, 31, 190, 22), regular, 15, Muted, TextAlignmentOptions.TopLeft);
        company.textWrappingMode = TextWrappingModes.NoWrap;
        company.overflowMode = TextOverflowModes.Ellipsis;
        company.text = "";
        var status = NewRect("Status", header);
        status.anchorMin = status.anchorMax = status.pivot = new Vector2(1, 0.5f);
        status.anchoredPosition = Vector2.zero;
        status.sizeDelta = new Vector2(170, 34);
        Round(status, Line, 17);
        var statusLabel = Txt(Stretch(NewRect("Label", status)), bold, 16, Muted, TextAlignmentOptions.Center);
        statusLabel.textWrappingMode = TextWrappingModes.NoWrap;
        statusLabel.text = "Still choosing";

        var cards = NewRect("Cards", section);
        var grid = cards.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(300, 420);
        grid.spacing = new Vector2(24, 26);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.UpperCenter;

        var empty = Txt(NewRect("Empty", section), regular, 16, Muted, TextAlignmentOptions.MidlineLeft);
        empty.fontStyle = FontStyles.Italic;
        empty.text = "No cards played yet.";
        empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 28;

        var line = NewRect("Line", section);
        line.gameObject.AddComponent<LayoutElement>().preferredHeight = 2;
        line.gameObject.AddComponent<Image>().color = Line;
        line.GetComponent<Image>().raycastTarget = false;

        section.gameObject.SetActive(false);
        return section.gameObject;
    }

    // Vertical gradient for the drawer, made once into Assets/Images/ActionCard/DrawerGradient.png.
    internal static Texture2D GetOrCreateGradient()
    {
        const string path = "Assets/Images/ActionCard/DrawerGradient.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing) return existing;

        const int w = 4, h = 256;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            var c = Color.Lerp(GradientBottom, GradientTop, Mathf.SmoothStep(0f, 1f, y / (h - 1f)));
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

    // Right-pointing chevron, made once into Assets/Images/ActionCard/ChevronRight.png.
    internal static Sprite GetOrCreateChevron()
    {
        const string path = "Assets/Images/ActionCard/ChevronRight.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing) return existing;

        const int size = 64;
        const float half = 4.5f; // stroke half-width
        var a = new Vector2(22, 54); var b = new Vector2(44, 32); var c = new Vector2(22, 10);
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            var p = new Vector2(x + 0.5f, y + 0.5f);
            float d = Mathf.Min(SegmentDistance(p, a, b), SegmentDistance(p, b, c));
            tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(half - d + 0.5f)));
        }
        tex.Apply();
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    // One tray item: a small card in the action's colour with its number.
    // Missing something: white inside, coloured number and an orange "!" (set by ActionDrawerView).
    private static GameObject BuildTrayItem(RectTransform parent)
    {
        var item = NewRect("TrayItemTemplate", parent);
        item.sizeDelta = new Vector2(42, 54);
        var outer = Round(item, Orange, 8);
        outer.raycastTarget = true;
        var button = item.gameObject.AddComponent<Button>();
        button.targetGraphic = outer;

        var inner = Round(Stretch(NewRect("Inner", item)), Color.white, 6);
        inner.rectTransform.offsetMin = new Vector2(3, 3);
        inner.rectTransform.offsetMax = new Vector2(-3, -3);
        inner.gameObject.SetActive(false);

        Txt(Stretch(NewRect("Number", item)), bold, 16, Color.white, TextAlignmentOptions.Center).text = "03";

        var alert = NewRect("Alert", item);
        alert.anchorMin = alert.anchorMax = new Vector2(1, 0);
        alert.pivot = new Vector2(0.5f, 0.5f);
        alert.sizeDelta = new Vector2(18, 18);
        alert.anchoredPosition = new Vector2(-3, 5);
        Round(alert, AlertOrange, 9);
        var ring = alert.gameObject.AddComponent<Outline>();
        ring.effectColor = new Color32(0xFF, 0xEC, 0xD2, 0xFF);
        ring.effectDistance = new Vector2(1.5f, -1.5f);
        Txt(Stretch(NewRect("Mark", alert)), bold, 13, Color.white, TextAlignmentOptions.Center).text = "!";
        alert.gameObject.SetActive(false);
        return item.gameObject;
    }

    // ScrollRect between header and footer; returns the grid the cards spawn into.
    private static RectTransform BuildScroll(RectTransform drawer, out ScrollRect sr)
    {
        var scroll = NewRect("Scroll", drawer);
        scroll.anchorMin = Vector2.zero;
        scroll.anchorMax = Vector2.one;
        scroll.offsetMin = new Vector2(0, FooterHeight);
        scroll.offsetMax = new Vector2(0, -HeaderHeight);

        var viewport = Stretch(NewRect("Viewport", scroll));
        viewport.offsetMax = new Vector2(-16, 0);
        viewport.gameObject.AddComponent<RectMask2D>();
        var hit = viewport.gameObject.AddComponent<Image>();
        hit.color = new Color(1, 1, 1, 0); // lets the wheel/drag scroll between cards

        var content = NewRect("Cards", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = Vector2.zero;
        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(300, 420);
        grid.spacing = new Vector2(24, 26);
        grid.padding = new RectOffset(20, 20, 22, 26);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.UpperCenter;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var bar = NewRect("Scrollbar", scroll);
        bar.anchorMin = new Vector2(1, 0);
        bar.anchorMax = new Vector2(1, 1);
        bar.pivot = new Vector2(1, 0.5f);
        bar.sizeDelta = new Vector2(8, -24);
        bar.anchoredPosition = new Vector2(-8, 0);
        Round(bar, Line, 4).raycastTarget = true;
        var area = Stretch(NewRect("Sliding Area", bar));
        var handle = Stretch(NewRect("Handle", area));
        var handleImg = Round(handle, new Color32(0xC9, 0xB7, 0x9F, 0xFF), 4);
        handleImg.raycastTarget = true;
        var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handleImg;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        sr = scroll.gameObject.AddComponent<ScrollRect>();
        sr.content = content;
        sr.viewport = viewport;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 40;
        sr.verticalScrollbar = scrollbar;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        return content;
    }

    // Four card backs fanned out from the right edge, with an "Actions 0/3" pill.
    // The card fan takes the instrument stack's place on the screen edge (same height), clear of the
    // Game Log tab further down. Without an instrument stack it keeps the default spot.
    private static void AlignWithInstrumentStack(RectTransform fan, RectTransform area)
    {
        foreach (var other in Object.FindObjectsByType<HandFan>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (other.name != "InstrumentStack" || other.gameObject.scene != area.gameObject.scene) continue;
            var stack = (RectTransform)other.transform;
            Vector3 centre = area.InverseTransformPoint(stack.TransformPoint(stack.rect.center));
            float y = centre.y - area.rect.center.y; // the fan is anchored at the middle of the area's right edge
            fan.anchoredPosition = new Vector2(fan.anchoredPosition.x, y);
            Debug.Log($"[ActionDrawerBuilder] Action card fan placed at the instrument stack's height ({y:0}).");
            return;
        }
        Debug.LogWarning("[ActionDrawerBuilder] No InstrumentStack found: run BioValue/Build Instrument Tray first to line the card fan up with it.");
    }

    // Moves the fan up until its lowest card clears the Game Log tab on the same edge (with a gap).
    private static void KeepAboveGameLogTab(RectTransform fan, UnityEngine.SceneManagement.Scene scene)
    {
        RectTransform tab = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var log = root.GetComponentInChildren<GameLogDrawer>(true);
            if (log) { tab = log.transform.Find("Drawer/Tab") as RectTransform ?? FindTab(log.transform); break; }
        }
        if (!tab) { Debug.LogWarning("[ActionDrawerBuilder] No Game Log tab found: the card fan keeps its height."); return; }

        Canvas.ForceUpdateCanvases();
        var parent = (RectTransform)fan.parent;
        var corners = new Vector3[4];
        tab.GetWorldCorners(corners);
        float tabTop = Mathf.Max(parent.InverseTransformPoint(corners[1]).y, parent.InverseTransformPoint(corners[2]).y);
        float lowest = float.MaxValue;
        foreach (var r in fan.GetComponentsInChildren<RectTransform>(true))
        {
            if (!r.name.StartsWith("CardBack")) continue;
            r.GetWorldCorners(corners);
            foreach (var c in corners) lowest = Mathf.Min(lowest, parent.InverseTransformPoint(c).y);
        }
        if (lowest == float.MaxValue) return;
        const float gap = 36f;
        float shift = tabTop + gap - lowest;
        if (shift <= 0f) return;
        fan.anchoredPosition += new Vector2(0, shift);
        Debug.Log($"[ActionDrawerBuilder] Card fan moved up {shift:0} to clear the Game Log tab.");
    }

    private static RectTransform FindTab(Transform log)
    {
        foreach (var t in log.GetComponentsInChildren<RectTransform>(true))
            if (t.name == "Tab") return t;
        return null;
    }

    private static HandFan BuildFan(Transform area, out TMP_Text countText)
    {
        var root = NewRect("ActionHandFan", area);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(1, 0.5f);
        root.sizeDelta = new Vector2(240, 380);
        root.anchoredPosition = new Vector2(0, -20);
        var hit = root.gameObject.AddComponent<Image>();
        hit.color = new Color(1, 1, 1, 0);
        var button = root.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        var fan = root.gameObject.AddComponent<HandFan>();

        var cards = new RectTransform[4];
        for (int i = 0; i < cards.Length; i++)
        {
            var card = NewRect("CardBack " + (i + 1), root);
            card.anchorMin = card.anchorMax = Vector2.zero;
            card.pivot = new Vector2(0.5f, 0);
            card.sizeDelta = new Vector2(190, 266);
            card.anchoredPosition = new Vector2(168, 34);
            card.localRotation = Quaternion.Euler(0, 0, fan.angles[i]);

            var frame = Round(Stretch(NewRect("Frame", card)), Orange, 16);
            var sh = frame.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.3f);
            sh.effectDistance = new Vector2(-3, -4);
            var paper = Round(Stretch(NewRect("Paper", card)), new Color32(0xFF, 0xF3, 0xE3, 0xFF), 12);
            paper.rectTransform.offsetMin = new Vector2(6, 6);
            paper.rectTransform.offsetMax = new Vector2(-6, -6);

            var emblem = NewRect("Emblem", card);
            emblem.anchorMin = emblem.anchorMax = new Vector2(0.5f, 0.5f);
            emblem.sizeDelta = new Vector2(62, 84);
            emblem.anchoredPosition = new Vector2(0, 18);
            Round(emblem, Orange, 12);
            Txt(Stretch(NewRect("Letter", emblem)), bold, 34, Color.white, TextAlignmentOptions.Center).text = "A";

            var label = Txt(NewRect("Label", card), bold, 17, Orange, TextAlignmentOptions.Center);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            label.rectTransform.sizeDelta = new Vector2(170, 26);
            label.rectTransform.anchoredPosition = new Vector2(0, -48);
            label.fontStyle = FontStyles.UpperCase;
            label.characterSpacing = 10;
            label.text = "Actions";
            cards[i] = card;
        }
        fan.cards = cards;

        // Pill: "Actions  0/3"
        var pill = NewRect("Pill", root);
        pill.anchorMin = pill.anchorMax = pill.pivot = new Vector2(0, 1);
        pill.sizeDelta = new Vector2(150, 42);
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
        pillLabel.text = "Actions";
        var bubble = NewRect("Count", pill);
        bubble.anchorMin = bubble.anchorMax = bubble.pivot = new Vector2(1, 0.5f);
        bubble.sizeDelta = new Vector2(52, 30);
        bubble.anchoredPosition = new Vector2(-6, 0);
        Round(bubble, Pink, 15);
        countText = Txt(Stretch(NewRect("Text", bubble)), bold, 15, new Color32(0x1E, 0x0A, 0x14, 0xFF), TextAlignmentOptions.Center);
        countText.text = "0/3";
        return fan;
    }

    // ── Layout helpers (positions in pixels from the parent's top edge) ──

    private static RectTransform TopBand(RectTransform rt, float top, float height)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(0, height);
        rt.anchoredPosition = new Vector2(0, -top);
        return rt;
    }

    // Full-width row with left/right insets.
    private static RectTransform TopRow(RectTransform rt, float left, float top, float right, float height)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 1);
        rt.offsetMin = new Vector2(left, -top - height);
        rt.offsetMax = new Vector2(-right, -top);
        return rt;
    }

    private static void Divider(RectTransform parent, bool atTop)
    {
        var line = NewRect("Divider", parent);
        line.anchorMin = new Vector2(0, atTop ? 1 : 0);
        line.anchorMax = new Vector2(1, atTop ? 1 : 0);
        line.pivot = new Vector2(0.5f, atTop ? 1 : 0);
        line.sizeDelta = new Vector2(-48, 2);
        line.anchoredPosition = Vector2.zero;
        var img = line.gameObject.AddComponent<Image>();
        img.color = Line;
        img.raycastTarget = false;
    }
}
