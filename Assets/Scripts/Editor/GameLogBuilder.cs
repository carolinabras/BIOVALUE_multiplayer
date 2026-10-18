using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static ActionCardBuilder;

// Game Log drawer on MainGame's canvas: a white "GAME LOG" tab on the right edge (where the old tab was)
// slides in a panel with the log as a timeline (icon per kind of event, round dividers, filter chips),
// plus the unread count on the tab and the new-event peek next to it. See GameLogDrawer.
// Hides the old CHATPANEL (old log text and the unused chat). Safe to run again.
public static class GameLogBuilder
{
    private const string ScenePath = "Assets/Scenes/MainGame.unity";
    private const string OldPanelName = "CHATPANEL";

    private const float PanelW = 520f;    // including the part kept off screen
    private const float Hidden = 30f;     // off screen on the right (square right edge)
    private const float TabW = 52f, TabTuck = 24f, TabH = 210f;
    private const float DrawerGap = 14f;  // between the drawer and the rows of capsules
    private const float Pad = 24f;

    private static readonly Color Purple = new Color32(0x46, 0x32, 0x7F, 0xFF);
    private static readonly Color PurpleMid = new Color32(0x6A, 0x55, 0xA8, 0xFF);
    private static readonly Color Lilac = new Color32(0xF1, 0xEC, 0xFB, 0xFF);
    private static readonly Color LilacLine = new Color32(0xEE, 0xEA, 0xF5, 0xFF);
    private static readonly Color Pink = new Color32(0xFE, 0x46, 0xA5, 0xFF);
    private static readonly Color PinkSoft = new Color32(0xFF, 0xF0, 0xF7, 0xFF);
    private static readonly Color Ink = new Color32(0x24, 0x1B, 0x3A, 0xFF);
    private static readonly Color Muted = new Color32(0x6E, 0x65, 0x80, 0xFF);
    private static readonly Color Grey = new Color32(0x9A, 0x92, 0xAC, 0xFF);

    [MenuItem("BioValue/Build Game Log Drawer")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Build Game Log Drawer", "Stop Play mode first: the builder edits the scene.", "OK");
            return;
        }
        if (!LoadArt())
        {
            EditorUtility.DisplayDialog("Build Game Log Drawer",
                "Missing art or font. Run BioValue/Build Instrument Piece and BioValue/Build Action Card first.", "OK");
            return;
        }

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
            EditorUtility.DisplayDialog("Build Game Log Drawer", "No player capsules found in MainGame.", "OK");
            return;
        }
        var root = (RectTransform)canvas.rootCanvas.transform;

        Transform t;
        while ((t = root.Find("GameLogDrawer")) != null) Object.DestroyImmediate(t.gameObject);

        // Where the old tab sat (distance of its centre from the top of the screen).
        var oldPanel = root.Find(OldPanelName);
        float tabCentre = root.rect.height - 342f;
        var oldTab = oldPanel ? oldPanel.Find("OpenChatButton") as RectTransform : null;
        if (oldTab)
        {
            Edges(root, oldTab, out float top, out float bottom);
            tabCentre = (top + bottom) / 2f;
        }

        CapsuleRows(root, out float topRowBottom, out float bottomRowTop);
        float drawerTop = topRowBottom + DrawerGap;
        float drawerH = Mathf.Max(600f, bottomRowTop - DrawerGap - drawerTop);
        tabCentre = Mathf.Clamp(tabCentre, drawerTop + TabH / 2f, drawerTop + drawerH - TabH / 2f);

        var drawer = BuildDrawer(root, drawerTop, drawerH, tabCentre);

        // Above the board, tray and drawers; below the details windows.
        int index = root.childCount - 1;
        foreach (var name in new[] { "InstrumentDetailsPanel", "PlayerDetailsPanel" })
        {
            var d = root.Find(name);
            if (d) index = Mathf.Min(index, d.GetSiblingIndex());
        }
        drawer.transform.SetSiblingIndex(index);

        if (oldPanel)
        {
            if (oldPanel.GetComponentInChildren<GameLog>(true))
            {
                // The log itself lives there: only hide the old visuals.
                foreach (var name in new[] { "LogPanel", "OpenChatButton", "ChatPanel" })
                {
                    var c = oldPanel.Find(name);
                    if (c) c.gameObject.SetActive(false);
                }
            }
            else oldPanel.gameObject.SetActive(false);
        }
        else Debug.LogWarning("[GameLogBuilder] Old CHATPANEL not found; nothing to hide.");

        EditorUtility.SetDirty(drawer);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[GameLogBuilder] Game Log drawer built in MainGame (scene saved).");
    }

    private static GameLogDrawer BuildDrawer(RectTransform root, float drawerTop, float drawerH, float tabCentre)
    {
        var holder = Stretch(NewRect("GameLogDrawer", root));
        var comp = holder.gameObject.AddComponent<GameLogDrawer>();

        // Dim behind the open drawer; a click on it closes the drawer.
        var dim = Stretch(NewRect("Dim", holder));
        var dimImg = dim.gameObject.AddComponent<Image>();
        dimImg.color = new Color(0.08f, 0.055f, 0.16f, 0.25f);
        var dimButton = dim.gameObject.AddComponent<Button>();
        dimButton.transition = Selectable.Transition.None;
        dim.gameObject.SetActive(false);

        // Drawer against the right edge, between the two rows of capsules. Starts closed.
        float visibleW = PanelW - Hidden;
        var drawer = NewRect("Drawer", holder);
        drawer.anchorMin = drawer.anchorMax = drawer.pivot = new Vector2(1, 1);
        drawer.sizeDelta = new Vector2(visibleW, drawerH);
        drawer.anchoredPosition = new Vector2(visibleW, -drawerTop);

        // Tab on the drawer's left edge, behind the panel so its right corners hide under it.
        var tab = NewRect("Tab", drawer);
        tab.anchorMin = tab.anchorMax = new Vector2(0, 1);
        tab.pivot = new Vector2(1, 0.5f);
        tab.anchoredPosition = new Vector2(TabTuck, -(tabCentre - drawerTop));
        tab.sizeDelta = new Vector2(TabW + TabTuck, TabH);
        var tabImg = Round(tab, Color.white, 16);
        tabImg.raycastTarget = true;
        var tabShadow = tab.gameObject.AddComponent<Shadow>();
        tabShadow.effectColor = new Color(0.06f, 0.03f, 0.16f, 0.3f);
        tabShadow.effectDistance = new Vector2(-4, -6);
        var tabButton = tab.gameObject.AddComponent<Button>();
        tabButton.targetGraphic = tabImg;
        var tabLabel = Txt(NewRect("Label", tab), bold, 18, Purple, TextAlignmentOptions.Center);
        tabLabel.rectTransform.anchorMin = tabLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        tabLabel.rectTransform.sizeDelta = new Vector2(160, TabW);
        tabLabel.rectTransform.anchoredPosition = new Vector2(-TabTuck / 2f, -12);
        tabLabel.rectTransform.localRotation = Quaternion.Euler(0, 0, 90);
        tabLabel.characterSpacing = 8;
        tabLabel.textWrappingMode = TextWrappingModes.NoWrap;
        tabLabel.text = "GAME LOG";
        var tabArrow = NewRect("Arrow", tab);
        tabArrow.anchorMin = tabArrow.anchorMax = new Vector2(0.5f, 1f);
        tabArrow.sizeDelta = new Vector2(18, 18);
        tabArrow.anchoredPosition = new Vector2(-TabTuck / 2f, -24);
        tabArrow.localRotation = Quaternion.Euler(0, 0, 180); // points left = "open me"
        var arrowImg = tabArrow.gameObject.AddComponent<Image>();
        arrowImg.sprite = ActionDrawerBuilder.GetOrCreateChevron();
        arrowImg.color = Purple;
        arrowImg.raycastTarget = false;

        // Unread count on the tab's outer top corner.
        var badge = NewRect("Unread", tab);
        badge.anchorMin = badge.anchorMax = new Vector2(0, 1);
        badge.pivot = new Vector2(0.5f, 0.5f);
        badge.anchoredPosition = new Vector2(4, -4);
        badge.sizeDelta = new Vector2(32, 32);
        Round(badge, Color.white, 16);
        var badgeFill = Stretch(NewRect("Fill", badge));
        badgeFill.offsetMin = new Vector2(3, 3);
        badgeFill.offsetMax = new Vector2(-3, -3);
        Round(badgeFill, Pink, 13);
        var badgeText = Txt(Stretch(NewRect("Count", badge)), bold, 16, Color.white, TextAlignmentOptions.Center);
        badgeText.textWrappingMode = TextWrappingModes.NoWrap;
        badgeText.text = "1";
        badge.gameObject.SetActive(false);

        // Panel
        var panel = Stretch(NewRect("Panel", drawer));
        panel.offsetMax = new Vector2(Hidden, 0);
        var panelImg = Round(panel, Color.white, 26);
        panelImg.raycastTarget = true;
        var panelShadow = panel.gameObject.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0.04f, 0.02f, 0.1f, 0.35f);
        panelShadow.effectDistance = new Vector2(-6, -10);
        panel.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        Column(panel, 0, new RectOffset(0, 0, 0, 0));

        // Header: title, count, filter chips
        var header = NewRect("Header", panel);
        Fixed(header, -1, 112);
        var headerImg = header.gameObject.AddComponent<Image>();
        headerImg.color = Lilac;
        headerImg.raycastTarget = false;
        float innerW = visibleW - 2 * Pad;
        var title = Txt(Place(NewRect("Title", header), Pad, 14, innerW, 40), bold, 30, Purple, TextAlignmentOptions.BottomLeft);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.text = "Game log";
        var count = Txt(Place(NewRect("Count", header), Pad, 14, innerW, 36), regular, 17, Muted, TextAlignmentOptions.BottomRight);
        count.textWrappingMode = TextWrappingModes.NoWrap;
        count.text = "0 events";
        var filters = Place(NewRect("Filters", header), Pad, 64, innerW, 32);
        var filterRow = Row(filters, 6, new RectOffset(0, 0, 0, 0));
        filterRow.childControlHeight = true;
        string[] filterNames = { "All", "Turns", "Board", "Votes", "Cards" };
        var chips = new Image[filterNames.Length];
        var chipLabels = new TMP_Text[filterNames.Length];
        for (int i = 0; i < filterNames.Length; i++)
        {
            // Not just the filter's name: DragPiece and others look up the board with GameObject.Find("Board").
            var chip = NewRect("Filter" + filterNames[i], filters);
            Fixed(chip, -1, 32);
            Row(chip, 0, new RectOffset(14, 14, 0, 0)).childAlignment = TextAnchor.MiddleCenter;
            chips[i] = Round(chip, i == 0 ? Purple : Color.white, 16);
            chips[i].raycastTarget = true;
            var button = chip.gameObject.AddComponent<Button>();
            button.targetGraphic = chips[i];
            chipLabels[i] = Txt(NewRect("Label", chip), bold, 16, i == 0 ? Color.white : Purple, TextAlignmentOptions.Center);
            chipLabels[i].textWrappingMode = TextWrappingModes.NoWrap;
            chipLabels[i].text = filterNames[i];
            UnityEventTools.AddIntPersistentListener(button.onClick, new UnityAction<int>(comp.SetFilter), i);
        }

        // Scrolling list
        var scrollRT = NewRect("Scroll", panel);
        scrollRT.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
        var viewport = Stretch(NewRect("Viewport", scrollRT));
        viewport.offsetMax = new Vector2(-Hidden, 0);
        viewport.gameObject.AddComponent<RectMask2D>();
        var hit = viewport.gameObject.AddComponent<Image>(); // lets the wheel/drag reach the scroll view
        hit.color = new Color(1, 1, 1, 0);
        var list = NewRect("List", viewport);
        list.anchorMin = new Vector2(0, 1);
        list.anchorMax = new Vector2(1, 1);
        list.pivot = new Vector2(0.5f, 1);
        list.offsetMin = list.offsetMax = Vector2.zero;
        Column(list, 0, new RectOffset(18, 18, 4, 20));
        list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = scrollRT.gameObject.AddComponent<ScrollRect>();
        scroll.content = list;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40;

        var divider = BuildDivider(list);
        var entry = BuildEntry(list);
        var empty = Txt(NewRect("Empty", list), regular, 18, Grey, TextAlignmentOptions.Center);
        empty.text = "Nothing has happened yet.";
        Fixed(empty.rectTransform, -1, 90);

        // Peek: the newest event, shown for a few seconds next to the closed tab.
        var slot = NewRect("PeekSlot", holder);
        slot.anchorMin = slot.anchorMax = new Vector2(1, 1);
        slot.pivot = new Vector2(1, 0.5f);
        slot.anchoredPosition = new Vector2(-(TabW + 12), -tabCentre);
        slot.sizeDelta = new Vector2(10, 10);
        var peek = NewRect("Peek", slot);
        peek.anchorMin = peek.anchorMax = peek.pivot = new Vector2(1, 0.5f);
        peek.anchoredPosition = Vector2.zero;
        var peekRow = Row(peek, 12, new RectOffset(8, 22, 8, 8));
        peekRow.childControlHeight = true;
        var fit = peek.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        Round(peek, Color.white, 27);
        var peekShadow = peek.gameObject.AddComponent<Shadow>();
        peekShadow.effectColor = new Color(0.06f, 0.03f, 0.16f, 0.35f);
        peekShadow.effectDistance = new Vector2(0, -8);
        var group = peek.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        var peekIcon = BuildIcon(peek, 38);
        var peekText = Txt(NewRect("Text", peek), medium, 19, Ink, TextAlignmentOptions.MidlineLeft);
        peekText.textWrappingMode = TextWrappingModes.NoWrap;
        peekText.text = "<b>Carla</b> placed <b>Land Use Zoning</b>";
        peek.gameObject.SetActive(false);

        comp.drawer = drawer;
        comp.dim = dim.gameObject;
        comp.tabArrow = tabArrow;
        comp.scroll = scroll;
        comp.list = list;
        comp.entryTemplate = entry;
        comp.dividerTemplate = divider;
        comp.emptyLabel = empty.gameObject;
        comp.countText = count;
        comp.filterChips = chips;
        comp.filterLabels = chipLabels;
        comp.unreadBadge = badge.gameObject;
        comp.unreadText = badgeText;
        comp.peek = group;
        comp.peekText = peekText;
        comp.peekIcon = peekIcon;
        UnityEventTools.AddPersistentListener(tabButton.onClick, new UnityAction(comp.Toggle));
        UnityEventTools.AddPersistentListener(dimButton.onClick, new UnityAction(comp.Close));
        return comp;
    }

    // "ROUND NAME ─────────"
    private static GameObject BuildDivider(RectTransform list)
    {
        var row = NewRect("DividerTemplate", list);
        var layout = Row(row, 10, new RectOffset(4, 4, 16, 4));
        layout.childControlHeight = true;
        var label = Txt(NewRect("Label", row), bold, 15, PurpleMid, TextAlignmentOptions.MidlineLeft);
        label.characterSpacing = 6;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.text = "INSTRUMENTS ROUND";
        var line = NewRect("Line", row);
        line.gameObject.AddComponent<Image>().color = LilacLine;
        var size = line.gameObject.AddComponent<LayoutElement>();
        size.flexibleWidth = 1;
        size.preferredHeight = size.minHeight = 2;
        row.gameObject.SetActive(false);
        return row.gameObject;
    }

    // Icon, sentence (+ chip) and time.
    private static GameObject BuildEntry(RectTransform list)
    {
        var row = NewRect("EntryTemplate", list);
        var layout = Row(row, 12, new RectOffset(8, 8, 11, 11));
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;

        var bg = Stretch(NewRect("Bg", row));
        bg.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        Round(bg, PinkSoft, 12);
        bg.gameObject.SetActive(false);
        var line = NewRect("Line", row);
        line.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        line.anchorMin = new Vector2(0, 0);
        line.anchorMax = new Vector2(1, 0);
        line.pivot = new Vector2(0.5f, 0);
        line.sizeDelta = new Vector2(0, 1.5f);
        line.gameObject.AddComponent<Image>().color = LilacLine;

        BuildIcon(row, 34);

        var body = NewRect("Body", row);
        Column(body, 4, new RectOffset(0, 0, 3, 0));
        body.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        var text = Txt(NewRect("Text", body), medium, 19, Ink, TextAlignmentOptions.TopLeft);
        text.text = "It’s <b>Carla</b>’s turn";
        var chipRow = NewRect("ChipRow", body);
        var chipLayout = Row(chipRow, 0, new RectOffset(0, 0, 0, 0));
        chipLayout.childControlHeight = true;
        var chip = NewRect("Chip", chipRow);
        var chipInner = Row(chip, 0, new RectOffset(10, 10, 1, 1));
        chipInner.childControlHeight = true;
        Round(chip, new Color32(0xE3, 0xF5, 0xE8, 0xFF), 12);
        Fixed(chip, -1, 24);
        var chipLabel = Txt(NewRect("Label", chip), bold, 15, new Color32(0x1E, 0x7A, 0x39, 0xFF), TextAlignmentOptions.Center);
        chipLabel.textWrappingMode = TextWrappingModes.NoWrap;
        chipLabel.text = "4 yes · 1 no";
        chipRow.gameObject.SetActive(false);

        var time = Txt(NewRect("Time", row), regular, 15, Grey, TextAlignmentOptions.TopRight);
        time.textWrappingMode = TextWrappingModes.NoWrap;
        time.text = "10:42";
        var timeSize = time.gameObject.AddComponent<LayoutElement>();
        timeSize.preferredWidth = timeSize.minWidth = 48;
        time.margin = new Vector4(0, 4, 0, 0);

        row.gameObject.SetActive(false);
        return row.gameObject;
    }

    // Coloured circle with one white glyph per kind of event (GameLogDrawer shows the right one).
    private static RectTransform BuildIcon(RectTransform parent, float size)
    {
        var icon = NewRect("Icon", parent);
        Fixed(icon, size, size);
        Round(icon, PurpleMid, size / 2f);
        float s = size / 34f;

        var turn = Glyph(icon, "Turn");
        var person = Place(NewRect("Person", turn), 0, 0, 0, 0);
        person.anchorMin = person.anchorMax = person.pivot = new Vector2(0.5f, 0.5f);
        person.anchoredPosition = new Vector2(0, -1 * s);
        person.sizeDelta = new Vector2(22, 22) * s;
        PlayerCapsuleBuilder.Icon(person, PlayerCapsuleBuilder.GetOrCreatePerson(), Color.white);

        var placed = Glyph(icon, "Placed");
        var hexRT = NewRect("Hex", placed);
        hexRT.anchorMin = hexRT.anchorMax = hexRT.pivot = new Vector2(0.5f, 0.5f);
        hexRT.sizeDelta = new Vector2(18, 20) * s;
        PlayerCapsuleBuilder.Icon(hexRT, hex, Color.white);

        var passed = Glyph(icon, "Passed");
        Bar(passed, new Vector2(-4.5f, -1.5f) * s, new Vector2(9, 3.6f) * s, -45f);
        Bar(passed, new Vector2(2.8f, 1.5f) * s, new Vector2(17, 3.6f) * s, 50f);

        var failed = Glyph(icon, "Failed");
        Bar(failed, Vector2.zero, new Vector2(16, 3.6f) * s, 45f);
        Bar(failed, Vector2.zero, new Vector2(16, 3.6f) * s, -45f);

        var cards = Glyph(icon, "Cards");
        Card(cards, new Vector2(-3, 1) * s, s, -14f, new Color(1, 1, 1, 0.65f));
        Card(cards, new Vector2(2, -1) * s, s, 8f, Color.white);

        var plus = Glyph(icon, "Plus");
        Bar(plus, Vector2.zero, new Vector2(15, 3.6f) * s, 0f);
        Bar(plus, Vector2.zero, new Vector2(15, 3.6f) * s, 90f);

        var minus = Glyph(icon, "Minus");
        Bar(minus, Vector2.zero, new Vector2(15, 3.6f) * s, 0f);

        var note = Glyph(icon, "Note");
        Bar(note, Vector2.zero, new Vector2(8, 8) * s, 0f);

        foreach (Transform g in icon) g.gameObject.SetActive(g.name == "Turn");
        return icon;
    }

    private static RectTransform Glyph(RectTransform icon, string name) => Stretch(NewRect(name, icon));

    private static void Bar(RectTransform parent, Vector2 pos, Vector2 size, float angle)
    {
        var bar = NewRect("Bar", parent);
        bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0.5f, 0.5f);
        bar.anchoredPosition = pos;
        bar.sizeDelta = size;
        bar.localRotation = Quaternion.Euler(0, 0, angle);
        Round(bar, Color.white, Mathf.Min(size.x, size.y) / 2f);
    }

    private static void Card(RectTransform parent, Vector2 pos, float s, float angle, Color color)
    {
        var card = NewRect("Card", parent);
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = pos;
        card.sizeDelta = new Vector2(11, 15) * s;
        card.localRotation = Quaternion.Euler(0, 0, angle);
        Round(card, color, 2.5f * s);
    }

    // ── Measuring ────────────────────────────────────────────────────────────

    // Distances from the top of the screen to a rect's top and bottom edges.
    private static void Edges(RectTransform root, RectTransform rt, out float top, out float bottom)
    {
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (var c in corners)
        {
            float y = root.InverseTransformPoint(c).y;
            minY = Mathf.Min(minY, y);
            maxY = Mathf.Max(maxY, y);
        }
        top = root.rect.yMax - maxY;
        bottom = root.rect.yMax - minY;
    }

    // Bottom edge of the top row of capsules and top edge of the bottom row (from the top of the screen).
    private static void CapsuleRows(RectTransform root, out float topRowBottom, out float bottomRowTop)
    {
        topRowBottom = 96f;
        bottomRowTop = root.rect.height - 96f;
        bool anyTop = false, anyBottom = false;
        foreach (var hook in Object.FindObjectsByType<PlayerCapsuleHook>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (hook.gameObject.scene != root.gameObject.scene || !hook.gameObject.activeInHierarchy) continue;
            Edges(root, (RectTransform)hook.transform, out float top, out float bottom);
            if ((top + bottom) / 2f < root.rect.height / 2f)
            {
                topRowBottom = anyTop ? Mathf.Max(topRowBottom, bottom) : bottom;
                anyTop = true;
            }
            else
            {
                bottomRowTop = anyBottom ? Mathf.Min(bottomRowTop, top) : top;
                anyBottom = true;
            }
        }
    }

    // ── Layout helpers ───────────────────────────────────────────────────────

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

    private static HorizontalLayoutGroup Row(RectTransform rt, float spacing, RectOffset padding)
    {
        var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = padding;
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        return layout;
    }

    private static void Fixed(RectTransform rt, float w, float h)
    {
        var size = rt.GetComponent<LayoutElement>();
        if (!size) size = rt.gameObject.AddComponent<LayoutElement>();
        if (w >= 0) size.preferredWidth = size.minWidth = w;
        if (h >= 0) size.preferredHeight = size.minHeight = h;
        rt.sizeDelta = new Vector2(w >= 0 ? w : rt.sizeDelta.x, h >= 0 ? h : rt.sizeDelta.y);
    }
}
