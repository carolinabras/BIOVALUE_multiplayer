using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static ActionCardBuilder;

// Builds the instrument details window (opened by a long press on a piece) on the main game's canvas.
// Called by InstrumentTrayBuilder ("BioValue/Build Instrument Tray"), which saves the scene.
public static class InstrumentDetailsBuilder
{
    private const float W = 1000f, H = 600f, LeftW = 360f;
    private const float ButtonW = 220f;
    private const float X = LeftW + 32f;          // right column
    private const float RightW = W - X - 34f;

    private static readonly Color Purple = new Color32(0x46, 0x32, 0x7F, 0xFF);
    private static readonly Color PurpleMid = new Color32(0x6A, 0x55, 0xA8, 0xFF);
    private static readonly Color Lilac = new Color32(0xF1, 0xEC, 0xFB, 0xFF);
    private static readonly Color Ink = new Color32(0x24, 0x1B, 0x3A, 0xFF);
    private static readonly Color Body = new Color32(0x3A, 0x31, 0x50, 0xFF);
    private static readonly Color Muted = new Color32(0x6E, 0x65, 0x80, 0xFF);

    public static InstrumentDetailsPanel Build(Transform canvas, InstrumentTrayView tray)
    {
        var holder = Stretch(NewRect("InstrumentDetailsPanel", canvas));
        holder.SetAsLastSibling();
        var panel = holder.gameObject.AddComponent<InstrumentDetailsPanel>();

        var root = Stretch(NewRect("Root", holder));
        var group = root.gameObject.AddComponent<CanvasGroup>();

        // Dimmed board; a tap outside the window closes it.
        var dim = Stretch(NewRect("Dim", root)).gameObject.AddComponent<Image>();
        dim.color = new Color(0.08f, 0.055f, 0.16f, 0.55f);
        var dimButton = dim.gameObject.AddComponent<Button>();
        dimButton.transition = Selectable.Transition.None;

        var shadow = NewRect("WindowShadow", root);
        Center(shadow, new Vector2(0, -12));
        Round(shadow, new Color(0.04f, 0.02f, 0.1f, 0.35f), 28);

        var window = NewRect("Window", root);
        Center(window, Vector2.zero);
        Round(window, Color.white, 28).raycastTarget = true; // clicks on the window don't close it
        window.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        // ── Left: the piece on its category colour ──
        var left = Place(NewRect("Left", window), 0, 0, LeftW, H);
        var leftFill = left.gameObject.AddComponent<Image>();
        leftFill.color = new Color32(0xD0, 0xEA, 0xF0, 0xFF);
        leftFill.raycastTarget = false;
        left.gameObject.AddComponent<RectMask2D>();
        var fade = Stretch(NewRect("Fade", left)).gameObject.AddComponent<RawImage>();
        fade.texture = GetOrCreateWhiteFade();
        fade.raycastTarget = false;
        var cascade = Stretch(NewRect("HexCascade", left)).gameObject.AddComponent<CardCascade>();
        cascade.cardSprite = hex;
        cascade.pixelsPerUnitMultiplier = 1f;
        cascade.cardSize = new Vector2(30, 34);
        cascade.color = new Color(1f, 1f, 1f, 0.35f);

        var ring = NewRect("BigHex", left);
        ring.anchorMin = ring.anchorMax = ring.pivot = new Vector2(0.5f, 1f);
        ring.sizeDelta = new Vector2(244, 282);
        ring.anchoredPosition = new Vector2(0, -70);
        var ringImg = HexImage(ring, new Color32(0x22, 0x8A, 0xA2, 0xFF));
        var ringShadow = ring.gameObject.AddComponent<Shadow>();
        ringShadow.effectColor = new Color(0.14f, 0.1f, 0.24f, 0.25f);
        ringShadow.effectDistance = new Vector2(0, -6);
        var fillRT = Stretch(NewRect("Fill", ring));
        fillRT.offsetMin = new Vector2(9, 10);
        fillRT.offsetMax = new Vector2(-9, -10);
        var fillImg = HexImage(fillRT, new Color32(0xD0, 0xEA, 0xF0, 0xFF));
        var badge = NewRect("Badge", fillRT);
        badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 0.5f);
        badge.sizeDelta = new Vector2(82, 42);
        badge.anchoredPosition = new Vector2(0, 54);
        var badgeImg = Round(badge, new Color32(0x17, 0x68, 0x7A, 0xFF), 21);
        var bigNumber = Txt(Stretch(NewRect("Number", badge)), bold, 26, Color.white, TextAlignmentOptions.Center);
        bigNumber.text = "27";
        var bigName = Txt(NewRect("Name", fillRT), medium, 25, Ink, TextAlignmentOptions.Center);
        bigName.rectTransform.anchorMin = bigName.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        bigName.rectTransform.sizeDelta = new Vector2(184, 116);
        bigName.rectTransform.anchoredPosition = new Vector2(0, -28);
        bigName.overflowMode = TextOverflowModes.Ellipsis;
        bigName.lineSpacing = -10;
        bigName.text = "Environmental Impact Assessment";

        var cats = NewRect("Categories", left);
        cats.anchorMin = cats.anchorMax = cats.pivot = new Vector2(0.5f, 1f);
        cats.sizeDelta = new Vector2(LeftW - 20, 100);
        cats.anchoredPosition = new Vector2(0, -380);
        var catsLayout = cats.gameObject.AddComponent<VerticalLayoutGroup>();
        catsLayout.spacing = 6;
        catsLayout.childAlignment = TextAnchor.UpperCenter;
        catsLayout.childControlWidth = true;
        catsLayout.childControlHeight = false;
        catsLayout.childForceExpandWidth = catsLayout.childForceExpandHeight = false;
        var catTemplate = Chip(cats, "CategoryTemplate", Color.white, Ink, true);

        // ── Right: header (eyebrow, name, status) stacks, so the status follows the name's height ──
        var header = Place(NewRect("Header", window), X, 30, RightW - 56, 0);
        var headerLayout = Column(header, 2);
        header.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var eyebrow = Txt(NewRect("Eyebrow", header), bold, 15, PurpleMid, TextAlignmentOptions.TopLeft);
        eyebrow.fontStyle = FontStyles.UpperCase;
        eyebrow.characterSpacing = 4;
        eyebrow.textWrappingMode = TextWrappingModes.NoWrap;
        eyebrow.text = "Instrument 27";
        var name = Txt(NewRect("Name", header), bold, 38, Ink, TextAlignmentOptions.TopLeft);
        name.lineSpacing = -8;
        name.text = "Environmental Impact Assessment";

        var close = NewRect("CloseX", window);
        close.anchorMin = close.anchorMax = close.pivot = new Vector2(1, 1);
        close.sizeDelta = new Vector2(40, 40);
        close.anchoredPosition = new Vector2(-24, -24);
        var closeImg = Round(close, Lilac, 20);
        closeImg.raycastTarget = true;
        var closeX = close.gameObject.AddComponent<Button>();
        closeX.targetGraphic = closeImg;
        foreach (float angle in new[] { 45f, -45f })
        {
            var bar = NewRect("Bar", close);
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0.5f);
            bar.sizeDelta = new Vector2(18, 3.5f);
            bar.localRotation = Quaternion.Euler(0, 0, angle);
            Round(bar, Purple, 1.75f);
        }

        // Where it is: "In the tray: you can place it now" / "On the board, placed by …"
        var statusRow = NewRect("StatusRow", header);
        statusRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 46;
        var status = Place(NewRect("Status", statusRow), 0, 10, 300, 34);
        Round(status, new Color32(0xF6, 0xF2, 0xFC, 0xFF), 17);
        var statusLayout = status.gameObject.AddComponent<HorizontalLayoutGroup>();
        statusLayout.padding = new RectOffset(12, 14, 0, 0);
        statusLayout.spacing = 8;
        statusLayout.childAlignment = TextAnchor.MiddleLeft;
        statusLayout.childControlWidth = true;
        statusLayout.childControlHeight = false;
        statusLayout.childForceExpandWidth = statusLayout.childForceExpandHeight = false;
        status.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        var statusDot = NewRect("Dot", status);
        statusDot.sizeDelta = new Vector2(10, 10);
        var statusDotImg = Round(statusDot, new Color32(0xFE, 0x46, 0xA5, 0xFF), 5);
        var dotSize = statusDot.gameObject.AddComponent<LayoutElement>();
        dotSize.preferredWidth = dotSize.minWidth = 10;
        var statusText = Txt(NewRect("Text", status), regular, 17, Body, TextAlignmentOptions.MidlineLeft);
        statusText.rectTransform.sizeDelta = new Vector2(200, 34);
        statusText.textWrappingMode = TextWrappingModes.NoWrap;
        statusText.text = "In the tray: you can place it now";

        // Description + links (scrolls)
        // Top is moved under the header at runtime (InstrumentDetailsPanel.FitBody).
        var scroll = Place(NewRect("Scroll", window), X, 190, RightW, H - 190 - 96);
        var viewport = Stretch(NewRect("Viewport", scroll));
        viewport.gameObject.AddComponent<RectMask2D>();
        var hit = viewport.gameObject.AddComponent<Image>();
        hit.color = new Color(1, 1, 1, 0);
        var content = NewRect("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = Vector2.zero;
        var contentLayout = Column(content, 8);
        contentLayout.padding = new RectOffset(0, 10, 0, 6);
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var sr = scroll.gameObject.AddComponent<ScrollRect>();
        sr.content = content;
        sr.viewport = viewport;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 40;

        var desc = Txt(NewRect("Description", content), regular, 21, Body, TextAlignmentOptions.TopLeft);
        desc.text = "Description";

        var links = NewRect("Links", content);
        var linksLayout = Column(links, 6);
        linksLayout.padding = new RectOffset(0, 0, 10, 0);
        var linksTitle = Txt(NewRect("Title", links), bold, 15, PurpleMid, TextAlignmentOptions.TopLeft);
        linksTitle.fontStyle = FontStyles.UpperCase;
        linksTitle.characterSpacing = 4;
        linksTitle.text = "Linked in action cards";
        var row = NewRect("Row", links);
        var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 6;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = false;
        var linkTemplate = Chip(row, "LinkTemplate", Purple, Color.white, false);
        var noLinks = Txt(NewRect("NoLinks", row), regular, 18, Muted, TextAlignmentOptions.MidlineLeft);
        noLinks.rectTransform.sizeDelta = new Vector2(300, 34);
        noLinks.fontStyle = FontStyles.Italic;
        noLinks.textWrappingMode = TextWrappingModes.NoWrap;
        noLinks.text = "Not linked to any action card yet";
        links.gameObject.SetActive(false);

        // ── Footer ──
        var divider = Place(NewRect("Divider", window), X, H - 80, RightW, 2).gameObject.AddComponent<Image>();
        divider.color = new Color32(0xEE, 0xE8, 0xF8, 0xFF);
        divider.raycastTarget = false;

        var prev = RoundButton(Place(NewRect("Previous", window), X, H - 62, 42, 42), 180f);
        var position = Txt(Place(NewRect("Position", window), X + 46, H - 62, 90, 42), bold, 17, Muted, TextAlignmentOptions.Center);
        position.textWrappingMode = TextWrappingModes.NoWrap;
        position.text = "1 of 12";
        var next = RoundButton(Place(NewRect("Next", window), X + 140, H - 62, 42, 42), 0f);

        var place = BottomRight(NewRect("PlaceButton", window), 34, 18, ButtonW, 54);
        var placeImg = Round(place, Purple, 14);
        placeImg.raycastTarget = true;
        var placeButton = place.gameObject.AddComponent<Button>();
        placeButton.targetGraphic = placeImg;
        Txt(Stretch(NewRect("Label", place)), bold, 21, Color.white, TextAlignmentOptions.Center).text = "Place on board";

        var closeB = BottomRight(NewRect("CloseButton", window), 34, 18, ButtonW, 54);
        var closeBImg = Round(closeB, Color.white, 14);
        closeBImg.raycastTarget = true;
        var outline = closeB.gameObject.AddComponent<Outline>();
        outline.effectColor = Purple;
        outline.effectDistance = new Vector2(2, -2);
        var closeButton = closeB.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeBImg;
        Txt(Stretch(NewRect("Label", closeB)), bold, 21, Purple, TextAlignmentOptions.Center).text = "Close";
        closeB.gameObject.SetActive(false);

        // Between the arrows and the button (may wrap to two lines, never over the arrows).
        const float hintLeft = X + 182 + 16, hintRight = 34 + ButtonW + 16;
        var hint = Txt(BottomRight(NewRect("Hint", window), hintRight, 18, W - hintLeft - hintRight, 54), regular, 16, Muted, TextAlignmentOptions.MidlineRight);
        hint.lineSpacing = -14;
        hint.text = "Then tap a highlighted cell";

        // ── Wire ──
        panel.root = root.gameObject;
        panel.rootGroup = group;
        panel.window = window;
        panel.tray = tray;
        panel.leftFill = leftFill;
        panel.bigRing = ringImg;
        panel.bigFill = fillImg;
        panel.bigBadge = badgeImg;
        panel.bigNumber = bigNumber;
        panel.bigName = bigName;
        panel.categoryContainer = cats;
        panel.categoryTemplate = catTemplate;
        panel.header = header;
        panel.eyebrow = eyebrow;
        panel.nameText = name;
        panel.statusDot = statusDotImg;
        panel.statusText = statusText;
        panel.description = desc;
        panel.scroll = sr;
        panel.linksBlock = links.gameObject;
        panel.linksContainer = row;
        panel.linkTemplate = linkTemplate;
        panel.noLinks = noLinks.gameObject;
        panel.positionText = position;
        panel.footHint = hint;
        panel.placeButton = place.gameObject;
        panel.closeButton = closeB.gameObject;
        root.gameObject.SetActive(false);
        EditorUtility.SetDirty(panel);

        UnityEventTools.AddPersistentListener(dimButton.onClick, new UnityAction(panel.Close));
        UnityEventTools.AddPersistentListener(closeX.onClick, new UnityAction(panel.Close));
        UnityEventTools.AddPersistentListener(closeButton.onClick, new UnityAction(panel.Close));
        UnityEventTools.AddPersistentListener(placeButton.onClick, new UnityAction(panel.PlaceOnBoard));
        UnityEventTools.AddPersistentListener(prev.onClick, new UnityAction(panel.Previous));
        UnityEventTools.AddPersistentListener(next.onClick, new UnityAction(panel.Next));
        return panel;
    }

    private static void Center(RectTransform rt, Vector2 offset)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(W, H);
        rt.anchoredPosition = offset;
    }

    private static RectTransform BottomRight(RectTransform rt, float right, float bottom, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(-right, bottom);
        return rt;
    }

    private static Image HexImage(RectTransform rt, Color color)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = hex;
        img.preserveAspect = true;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static VerticalLayoutGroup Column(RectTransform rt, float spacing)
    {
        var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return layout;
    }

    // Pill with an optional colour dot and a label; the panel clones it.
    private static GameObject Chip(RectTransform parent, string name, Color fill, Color text, bool withDot)
    {
        var chip = NewRect(name, parent);
        chip.sizeDelta = new Vector2(120, 34);
        Round(chip, fill, 17);
        if (withDot)
        {
            var sh = chip.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.14f, 0.1f, 0.24f, 0.12f);
            sh.effectDistance = new Vector2(0, -2);
        }
        var layout = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 14, 0, 0);
        layout.spacing = 6;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        if (withDot)
        {
            var dot = NewRect("Dot", chip);
            dot.sizeDelta = new Vector2(11, 11);
            Round(dot, Purple, 5.5f);
            var size = dot.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = size.minWidth = 11;
        }
        var label = Txt(NewRect("Label", chip), bold, 16, text, TextAlignmentOptions.MidlineLeft);
        label.rectTransform.sizeDelta = new Vector2(80, 34);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.text = "Label";
        chip.gameObject.SetActive(false);
        return chip.gameObject;
    }

    // Lilac circle with a chevron (0° points right).
    private static Button RoundButton(RectTransform rt, float rotation)
    {
        var img = Round(rt, Lilac, 19);
        img.raycastTarget = true;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        var arrow = NewRect("Arrow", rt);
        arrow.anchorMin = arrow.anchorMax = arrow.pivot = new Vector2(0.5f, 0.5f);
        arrow.sizeDelta = new Vector2(18, 18);
        arrow.localRotation = Quaternion.Euler(0, 0, rotation);
        var arrowImg = arrow.gameObject.AddComponent<Image>();
        arrowImg.sprite = ActionDrawerBuilder.GetOrCreateChevron();
        arrowImg.color = Purple;
        arrowImg.raycastTarget = false;
        return button;
    }

    // Transparent at the top to white at the bottom, made once into Assets/Images/InstrumentPiece/WhiteFade.png.
    private static Texture2D GetOrCreateWhiteFade()
    {
        const string path = "Assets/Images/InstrumentPiece/WhiteFade.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing) return existing;

        const int w = 4, h = 256;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            float a = Mathf.SmoothStep(0f, 1f, 1f - y / (h - 1f)); // white at the bottom
            for (int x = 0; x < w; x++) tex.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        tex.Apply();
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
