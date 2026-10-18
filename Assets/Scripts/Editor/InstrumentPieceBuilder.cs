using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// Builds the new instrument piece design (front + back hexagon) into the Instrument prefab.
// Safe to run again: it rebuilds the "Piece" object and keeps your category colours.
public static class InstrumentPieceBuilder
{
    private const string PrefabPath = "Assets/Resources/PhotonPrefabs/Instrument.prefab";
    private const string ArtFolder = "Assets/Images/InstrumentPiece/";
    // Atkinson Hyperlegible Bold (free, OFL licence), made into a TextMeshPro font on first build.
    private const string FontTtf = "Assets/Fonts/AtkinsonHyperlegible/AtkinsonHyperlegible-Bold.ttf";
    private const string FontPath = "Assets/Fonts/AtkinsonHyperlegible/AtkinsonHyperlegible-Bold SDF.asset";

    // Letters baked into the font up front (English + Portuguese + common punctuation).
    // The font is dynamic, so anything else is still added when it's first used.
    private const string Charset =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
        "ÀÁÂÃÄÇÈÉÊËÌÍÎÏÑÒÓÔÕÖÙÚÛÜàáâãäçèéêëìíîïñòóôõöùúûüºª°«»–—‘’“”…€";

    internal static TMP_FontAsset GetOrCreateFont() => GetOrCreateFont(FontTtf, FontPath);

    internal static TMP_FontAsset GetOrCreateFont(string ttfPath, string assetPath)
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing) return existing;

        var ttf = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (!ttf) return null;

        var fa = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                                               AtlasPopulationMode.Dynamic, true);
        if (!fa) return null;
        fa.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        fa.TryAddCharacters(Charset, out _);

        AssetDatabase.CreateAsset(fa, assetPath);
        fa.atlasTextures[0].name = fa.name + " Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        fa.material.name = fa.name + " Material";
        // Narrower soft edge: titles are small on screen and look blurry at the default 0.
        fa.material.SetFloat(ShaderUtilities.ID_Sharpness, 0.5f);
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        return fa;
    }

    private static readonly Color TextDark = new Color32(0x1E, 0x1E, 0x1E, 0xFF);

    // Border thickness in design units (piece is 300x346). Was 10/12.
    private const float BorderX = 6f;
    private const float BorderY = 7f;

    [MenuItem("BioValue/Build Instrument Piece")]
    public static void Build()
    {
        var hex = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "HexFill.png");
        var circle = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "Circle.png");
        var font = GetOrCreateFont();
        if (!hex || !circle || !font)
        {
            EditorUtility.DisplayDialog("Build Instrument Piece",
                "Missing art or font. Check that Assets/Images/InstrumentPiece has HexFill.png and Circle.png (imported as Sprites) and that Assets/Fonts/AtkinsonHyperlegible has AtkinsonHyperlegible-Bold.ttf.", "OK");
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var view = root.GetComponent<InstrumentPieceView>() ?? root.AddComponent<InstrumentPieceView>();

            // On a rebuild the close button lives inside the old Piece: lift it out before deleting.
            var oldClose = root.transform.Find("Piece/CloseInstrumentButton");
            if (oldClose) oldClose.SetParent(root.transform, false);

            var old = root.transform.Find("Piece");
            if (old) Object.DestroyImmediate(old.gameObject);

            // Old visuals: keep the objects (InstrumentHook still writes to them) but hide them.
            foreach (var name in new[] { "Container", "Container (1)", "Container (2)" })
            {
                var t = root.transform.Find(name);
                if (t) t.gameObject.SetActive(false);
            }

            // The root image stays as an invisible hit area for dragging.
            var rootImage = root.GetComponent<Image>();
            if (rootImage) rootImage.color = new Color(1f, 1f, 1f, 0f);

            // ── Piece: fixed 300x346 design, scaled to fit by InstrumentPieceView ──
            var piece = NewRect("Piece", root.transform);
            piece.anchorMin = piece.anchorMax = new Vector2(0.5f, 0.5f);
            piece.pivot = new Vector2(0.5f, 0.5f);
            piece.sizeDelta = new Vector2(InstrumentPieceView.RefWidth, InstrumentPieceView.RefHeight);
            piece.anchoredPosition = Vector2.zero;

            // Texts: boxes are proportional (anchors only) and font sizes are real screen sizes,
            // so the global TextSupersampler sharpens them like every other text in the game.

            var flipper = Stretch(NewRect("Flipper", piece));

            // ── Front ──
            var front = Stretch(NewRect("Front", flipper));
            var fRing = HexImage("Ring", front, hex, true);
            var fRingSplit = SplitImage("RingSplit", front, hex);
            var fFill = Inset(HexImage("Fill", front, hex, false), BorderX, BorderY);
            var fFillSplit = Inset(SplitImage("FillSplit", front, hex), BorderX, BorderY);

            var badgeRT = NewRect("Badge", front);
            badgeRT.anchorMin = badgeRT.anchorMax = new Vector2(0.5f, 1f);
            badgeRT.pivot = new Vector2(0.5f, 0.5f);
            badgeRT.sizeDelta = new Vector2(50, 50);
            badgeRT.anchoredPosition = new Vector2(0, -57);
            var badge = badgeRT.gameObject.AddComponent<Image>();
            badge.sprite = circle;
            badge.raycastTarget = false;

            var number = Text("Number", Stretch(NewRect("Number", badgeRT)), font, 10, 15, Color.white);

            // Font sizes below are real screen sizes on a ~160x170 board cell.
            // Reaches down into the space the front icons used to take (they're hidden, see below).
            var titleRT = AnchorBox(NewRect("Title", front), 0.065f, 0.19f, 0.935f, 0.735f);
            var title = Text("Title", titleRT, ActionCardBuilder.MediumFont() ?? font, 13, 23, TextDark);
            title.text = "Instrument title";
            title.lineSpacing = 0;

            // Small category icon(s) under the title.
            var frontIconsRT = NewRect("Icons", front);
            frontIconsRT.anchorMin = frontIconsRT.anchorMax = new Vector2(0.5f, 0f);
            frontIconsRT.pivot = new Vector2(0.5f, 0.5f);
            frontIconsRT.sizeDelta = new Vector2(140, 46);
            frontIconsRT.anchoredPosition = new Vector2(0, 52);
            var frontRow = frontIconsRT.gameObject.AddComponent<HorizontalLayoutGroup>();
            frontRow.spacing = 10;
            frontRow.childAlignment = TextAnchor.MiddleCenter;
            frontRow.childControlWidth = frontRow.childControlHeight = false;
            frontRow.childForceExpandWidth = frontRow.childForceExpandHeight = false;
            var frontIcon1 = IconImage("Icon1", frontIconsRT);
            var frontIcon2 = IconImage("Icon2", frontIconsRT);
            // Hidden so the title can be bigger: the colour already says the category, and the back
            // and the details window show the icons. Switch this object back on to bring them back.
            frontIconsRT.gameObject.SetActive(false);

            // ── Back ──
            var back = Stretch(NewRect("Back", flipper));
            var bRing = HexImage("Ring", back, hex, true);
            var bRingSplit = SplitImage("RingSplit", back, hex);
            var bFill = Inset(HexImage("Fill", back, hex, false), BorderX, BorderY);
            var bFillSplit = Inset(SplitImage("FillSplit", back, hex), BorderX, BorderY);

            var iconsRT = NewRect("Icons", back);
            iconsRT.anchorMin = iconsRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconsRT.sizeDelta = new Vector2(260, 112);
            iconsRT.anchoredPosition = new Vector2(0, 24);
            var row = iconsRT.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 20;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = false;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var icon1 = IconImage("Icon1", iconsRT);
            var icon2 = IconImage("Icon2", iconsRT);

            var nameRT = AnchorBox(NewRect("CategoryName", back), 0.07f, 0.19f, 0.93f, 0.38f);
            var categoryName = Text("CategoryName", nameRT, font, 10, 15, TextDark);
            categoryName.fontStyle = FontStyles.UpperCase;
            categoryName.characterSpacing = 4;
            categoryName.text = "Category";
            back.gameObject.SetActive(false);

            // Close button: moved into the piece (so it scales with it, but doesn't flip) and restyled.
            var close = root.transform.Find("CloseInstrumentButton");
            if (close) StyleCloseButton((RectTransform)close, piece, circle);

            // ── Wire the view ──
            view.piece = piece;
            view.flipper = flipper;
            view.front = front.gameObject;
            view.back = back.gameObject;
            view.frontRing = fRing; view.frontRingSplit = fRingSplit;
            view.frontFill = fFill; view.frontFillSplit = fFillSplit;
            view.badge = badge; view.number = number; view.title = title;
            view.frontIcon1 = frontIcon1; view.frontIcon2 = frontIcon2;
            // Bigger shared title size; the few names that don't fit at it shrink on their own.
            view.titleMaxRatio = 0.26f;
            view.titleMinRatio = 0.21f;
            view.backRing = bRing; view.backRingSplit = bRingSplit;
            view.backFill = bFill; view.backFillSplit = bFillSplit;
            view.icon1 = icon1; view.icon2 = icon2;
            view.categoryName = categoryName;

            AssignIcon(view, InstrumentType.Inspection, "Icon_fiscalizacao.png");
            AssignIcon(view, InstrumentType.Planning, "Icon_planning.png");
            AssignIcon(view, InstrumentType.Incentive, "Icon_incentivo.png");
            AssignIcon(view, InstrumentType.Agreement, "Icon_acordo.png");
            AssignIcon(view, InstrumentType.Information, "Icon_informacao.png");
            AssignIcon(view, InstrumentType.Regulation, "Icon_regulacao.png");

            // Preview in the prefab: the first category.
            view.Apply(new Instrument { id = 1, name = "Instrument title", typeOne = InstrumentType.Inspection });

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("[InstrumentPieceBuilder] Instrument prefab rebuilt with the new piece design.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // Round grey button with a white ×, a white outline so it reads on every category colour,
    // and a red hover. Sits on the hexagon's top-right edge.
    private static void StyleCloseButton(RectTransform close, RectTransform piece, Sprite circle)
    {
        var xIcon = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "IconClose.png");

        close.SetParent(piece, false);
        close.SetAsLastSibling();
        close.anchorMin = close.anchorMax = new Vector2(1f, 1f);
        close.pivot = new Vector2(0.5f, 0.5f);
        close.sizeDelta = new Vector2(54, 54);
        close.anchoredPosition = new Vector2(-64, -72);
        close.localScale = Vector3.one;

        // Remove what a previous build added; hide the old text child.
        foreach (var n in new[] { "Disc", "Icon" })
        {
            var t = close.Find(n);
            if (t) Object.DestroyImmediate(t.gameObject);
        }
        for (int i = 0; i < close.childCount; i++)
            close.GetChild(i).gameObject.SetActive(false);

        // Outline (the button's own image): white circle with a soft shadow.
        var outline = close.GetComponent<Image>() ?? close.gameObject.AddComponent<Image>();
        outline.sprite = circle;
        outline.color = Color.white;
        outline.type = Image.Type.Simple;
        outline.preserveAspect = true;
        outline.raycastTarget = true;
        var shadow = close.GetComponent<Shadow>() ?? close.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
        shadow.effectDistance = new Vector2(0f, -3f);

        // Disc: the coloured part, tinted by the button states.
        var discRT = Stretch(NewRect("Disc", close));
        discRT.offsetMin = new Vector2(4, 4);
        discRT.offsetMax = new Vector2(-4, -4);
        var disc = discRT.gameObject.AddComponent<Image>();
        disc.sprite = circle;
        disc.raycastTarget = false;

        var iconRT = NewRect("Icon", close);
        iconRT.anchorMin = iconRT.anchorMax = new Vector2(0.5f, 0.5f);
        iconRT.sizeDelta = new Vector2(28, 28);
        var icon = iconRT.gameObject.AddComponent<Image>();
        icon.sprite = xIcon;
        icon.raycastTarget = false;

        var button = close.GetComponent<Button>();
        if (button)
        {
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = disc;
            var colors = button.colors;
            colors.normalColor = new Color32(0x7A, 0x80, 0x88, 0xFF); // grey
            colors.selectedColor = colors.normalColor;
            colors.highlightedColor = new Color32(0xC6, 0x28, 0x28, 0xFF);
            colors.pressedColor = new Color32(0x8E, 0x1B, 0x1B, 0xFF);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }
    }

    private static void AssignIcon(InstrumentPieceView view, InstrumentType type, string file)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + file);
        foreach (var c in view.categories)
            if (c != null && c.type == type) c.icon = sprite;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5; // UI
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    // Box given as fractions of the parent, so it resizes with the piece without any code.
    private static RectTransform AnchorBox(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
    {
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static RectTransform Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static Image Inset(Image img, float x, float y)
    {
        img.rectTransform.offsetMin = new Vector2(x, y);
        img.rectTransform.offsetMax = new Vector2(-x, -y);
        return img;
    }

    private static Image HexImage(string name, RectTransform parent, Sprite hex, bool raycast)
    {
        var img = Stretch(NewRect(name, parent)).gameObject.AddComponent<Image>();
        img.sprite = hex;
        img.raycastTarget = raycast;
        return img;
    }

    // Right half of the hexagon in a second colour, for instruments with two categories.
    private static Image SplitImage(string name, RectTransform parent, Sprite hex)
    {
        var img = HexImage(name, parent, hex, false);
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Right;
        img.fillAmount = 0.5f;
        img.gameObject.SetActive(false);
        return img;
    }

    private static Image IconImage(string name, RectTransform parent)
    {
        var rt = NewRect(name, parent);
        rt.sizeDelta = new Vector2(112, 112);
        var img = rt.gameObject.AddComponent<Image>();
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI Text(string name, RectTransform rt, TMP_FontAsset font, float min, float max, Color color)
    {
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font;
        t.fontStyle = FontStyles.Normal; // the font file is already Bold; no fake bold needed
        t.enableAutoSizing = true;
        t.fontSizeMin = min;
        t.fontSizeMax = max;
        t.fontSize = max;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.lineSpacing = -6;
        t.raycastTarget = false;
        return t;
    }
}
