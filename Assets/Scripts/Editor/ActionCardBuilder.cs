using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Builds the new action card design into ActionCard.prefab (and the "Add open card" tile).
// Safe to run again: it rebuilds the "Card" object and keeps the collaboration button/containers.
// Layout is a fixed 300x420 design, scaled to the cell by FitChildToRect.
[InitializeOnLoad]
public static class ActionCardBuilder
{
    // Builds once automatically after this design version first compiles; the menu item rebuilds any time.
    private const string AutoBuildKey = "BioValue.ActionCardBuilder.v13";

    static ActionCardBuilder()
    {
        if (Application.isBatchMode || EditorPrefs.GetBool(AutoBuildKey, false)) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorPrefs.SetBool(AutoBuildKey, true);
            Build();
        };
    }

    private const string PrefabPath = "Assets/Resources/PhotonPrefabs/ActionCard.prefab";
    private const string AddTilePath = "Assets/Resources/ActionCardAddTile.prefab";
    private const string ArtFolder = "Assets/Images/ActionCard/";
    private const string HexPath = "Assets/Images/InstrumentPiece/HexFill.png";
    private const string RegularTtf = "Assets/Fonts/AtkinsonHyperlegible/AtkinsonHyperlegible-Regular.ttf";
    private const string RegularFontPath = "Assets/Fonts/AtkinsonHyperlegible/AtkinsonHyperlegible-Regular SDF.asset";
    // Baloo 2 (free, OFL licence) for the action cards and the drawer; Atkinson is the fallback.
    private const string BalooBoldTtf = "Assets/Fonts/Baloo2/Baloo2-ExtraBold.ttf";
    private const string BalooBoldPath = "Assets/Fonts/Baloo2/Baloo2-ExtraBold SDF.asset";
    private const string BalooMediumTtf = "Assets/Fonts/Baloo2/Baloo2-Medium.ttf";
    private const string BalooMediumPath = "Assets/Fonts/Baloo2/Baloo2-Medium SDF.asset";
    private const string BalooRegularTtf = "Assets/Fonts/Baloo2/Baloo2-Regular.ttf";
    private const string BalooRegularPath = "Assets/Fonts/Baloo2/Baloo2-Regular SDF.asset";

    private const float W = 300f, H = 420f;
    private const int SpriteRadius = 24;

    private static readonly Color Ink = new Color32(0x1E, 0x2B, 0x27, 0xFF);
    private static readonly Color Muted = new Color32(0x4A, 0x5A, 0x55, 0xFF);
    private static readonly Color Orange = new Color32(0xB5, 0x65, 0x0A, 0xFF);
    private static readonly Color PinkDark = new Color32(0x8E, 0x12, 0x53, 0xFF);
    private static readonly Color SelectedPink = new Color32(0xFE, 0x46, 0xA5, 0xFF);
    private static readonly Color Line = new Color32(0xE1, 0xE6, 0xE3, 0xFF);
    private static readonly Color ButtonLight = new Color32(0xE9, 0xEE, 0xEB, 0xFF);
    private static readonly Color Collab = new Color32(0x2F, 0x6F, 0xB0, 0xFF);

    internal static Sprite round, hex;
    internal static TMP_FontAsset bold, medium, regular;

    [MenuItem("BioValue/Build Action Card")]
    public static void Build()
    {
        if (!LoadArt())
        {
            EditorUtility.DisplayDialog("Build Action Card",
                "Missing art or font. Run BioValue/Build Instrument Piece first (it creates the Atkinson font) and check Assets/Images/InstrumentPiece/HexFill.png.", "OK");
            return;
        }

        BuildCard();
        BuildAddTile();
        AssetDatabase.SaveAssets();
        Debug.Log("[ActionCardBuilder] ActionCard prefab and ActionCardAddTile rebuilt.");
    }

    // Rounded sprite, hexagon and Atkinson fonts used by the action card and drawer builders.
    internal static bool LoadArt()
    {
        round = GetOrCreateRoundRect();
        hex = AssetDatabase.LoadAssetAtPath<Sprite>(HexPath);
        bold = TightenBaloo(InstrumentPieceBuilder.GetOrCreateFont(BalooBoldTtf, BalooBoldPath)) ?? InstrumentPieceBuilder.GetOrCreateFont();
        regular = TightenBaloo(InstrumentPieceBuilder.GetOrCreateFont(BalooRegularTtf, BalooRegularPath))
                  ?? InstrumentPieceBuilder.GetOrCreateFont(RegularTtf, RegularFontPath) ?? bold;
        medium = MediumFont() ?? bold;
        return round && hex && bold;
    }

    // Baloo 2 Medium (instrument titles), or null if the font file is missing.
    internal static TMP_FontAsset MediumFont() =>
        TightenBaloo(InstrumentPieceBuilder.GetOrCreateFont(BalooMediumTtf, BalooMediumPath));

    // Baloo 2 reserves room for Devanagari, so its lines are 1.6x the font size (Atkinson: 1.24x).
    // Labels sized for Atkinson then don't fit and collapse to "…". Latin letters only need about
    // 1.0 above and 0.32 below the baseline, so the font asset uses those instead.
    private static TMP_FontAsset TightenBaloo(TMP_FontAsset font)
    {
        if (!font) return null;
        var face = font.faceInfo;
        float em = face.pointSize;
        if (face.ascentLine <= em * 1.01f) return font; // already done
        face.ascentLine = em * 1.0f;
        face.descentLine = -em * 0.32f;
        face.lineHeight = em * 1.32f;
        font.faceInfo = face;
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        return font;
    }

    private static void BuildCard()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var rootT = root.transform;
            var hook = root.GetComponent<ActionCardsHook>();

            // Parts we keep (scripts point at them). On a rebuild they live inside the old Card.
            var collabButton = FindDeep(rootT, "Button");
            var collabContainer = FindDeep(rootT, "prefabcontainer");
            var ownerContainer = FindDeep(rootT, "OwnerContainer");
            foreach (var keep in new[] { collabButton, collabContainer, ownerContainer })
                if (keep) keep.SetParent(rootT, false);

            foreach (var name in new[] { "Card", "Action_DescriptionHow", "Action_Description", "Action_DescriptionCustom",
                                         "Container", "Container (1)", "DescritionCustom", "Description How" })
            {
                var t = rootT.Find(name);
                if (t) Object.DestroyImmediate(t.gameObject);
            }

            // Old root look: hidden. Selection is the Select button now, not a click anywhere.
            var rootImage = root.GetComponent<Image>();
            if (rootImage) rootImage.enabled = false;
            var rootButton = root.GetComponent<Button>();
            if (rootButton) rootButton.enabled = false;
            var outline = root.GetComponent<Outline>();
            if (outline) outline.enabled = false;
            var group = root.GetComponent<CanvasGroup>() ?? root.AddComponent<CanvasGroup>();

            // ── Card ──
            var card = NewRect("Card", rootT);
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(W, H);
            card.anchoredPosition = Vector2.zero;
            var fit = root.GetComponent<FitChildToRect>() ?? root.AddComponent<FitChildToRect>();
            fit.content = card;
            fit.referenceSize = new Vector2(W, H);

            var glow = Round(Stretch(NewRect("Glow", card)), SelectedPink, 24);
            glow.rectTransform.offsetMin = new Vector2(-7, -7);
            glow.rectTransform.offsetMax = new Vector2(7, 7);
            glow.gameObject.SetActive(false);

            var frame = Round(Stretch(NewRect("Frame", card)), Orange, 18);
            var shadow = frame.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.3f);
            shadow.effectDistance = new Vector2(0, -5);
            var paper = Round(Stretch(NewRect("Paper", card)), new Color32(0xFB, 0xF5, 0xEC, 0xFF), 15);
            paper.rectTransform.offsetMin = new Vector2(4, 4);
            paper.rectTransform.offsetMax = new Vector2(-4, -4);

            // Header: number on a small upright card (hexagons are for instruments only) + kind.
            var badgeRT = Place(NewRect("Badge", card), 16, 14, 44, 58);
            var badge = Round(badgeRT, Orange, 9);
            var number = Txt(Stretch(NewRect("Number", badgeRT)), bold, 20, Color.white, TextAlignmentOptions.Center);
            number.text = "01";

            var kind = Txt(Place(NewRect("Kind", card), 120, 30, 164, 22), bold, 13, Orange, TextAlignmentOptions.Right);
            kind.fontStyle = FontStyles.UpperCase;
            kind.characterSpacing = 8;
            kind.text = "Action";

            if (ownerContainer)
            {
                Place((RectTransform)ownerContainer, 74, 54, 210, 24);
                ownerContainer.SetAsLastSibling();
            }

            // ── Front ──
            var front = Place(NewRect("Front", card), 16, 82, 268, 164);
            var desc = Txt(Stretch(NewRect("Description", front)), bold, 24, Ink, TextAlignmentOptions.TopLeft);
            desc.enableAutoSizing = true;
            desc.fontSizeMin = 15;
            desc.fontSizeMax = 24;
            desc.lineSpacing = -4;
            desc.overflowMode = TextOverflowModes.Ellipsis;
            desc.text = "Action description";

            // Open cards: the label is a child of the input so the hook hides both together.
            var customRT = Place(NewRect("CustomInput", front), 0, 22, 268, 142);
            var customLabel = Txt(Place(NewRect("Label", customRT), 0, -22, 268, 18), bold, 13, PinkDark, TextAlignmentOptions.TopLeft);
            customLabel.text = "Your action";
            var customInput = Input("CustomInput", customRT,
                bold, 18, new Color32(0xFF, 0xEC, 0xF5, 0xFF), "Define your own action", 120);

            // ── Back ──
            var back = Place(NewRect("Back", card), 16, 82, 268, 164);
            var howTitle = Txt(Place(NewRect("HowTitle", back), 0, 0, 268, 38), bold, 15, Ink, TextAlignmentOptions.TopLeft);
            howTitle.enableAutoSizing = true;
            howTitle.fontSizeMin = 11;
            howTitle.fontSizeMax = 15;
            howTitle.overflowMode = TextOverflowModes.Ellipsis;
            howTitle.text = "Action description";
            var howHeader = Txt(Place(NewRect("HowHeader", back), 0, 40, 268, 18), bold, 14, Muted, TextAlignmentOptions.TopLeft);
            howHeader.text = "How will you do it?";
            var howInput = Input("HowInput", Place(NewRect("HowInput", back), 0, 60, 268, 104),
                regular, 17, Color.white, "Describe your action: who, where, when…", 280);
            back.gameObject.SetActive(false);

            // ── Played (read-only) ──
            var played = Place(NewRect("Played", card), 16, 82, 268, 164);
            var playedDesc = Txt(Place(NewRect("PlayedDescription", played), 0, 0, 268, 64), bold, 19, Ink, TextAlignmentOptions.TopLeft);
            playedDesc.enableAutoSizing = true;
            playedDesc.fontSizeMin = 12;
            playedDesc.fontSizeMax = 19;
            playedDesc.lineSpacing = -4;
            playedDesc.overflowMode = TextOverflowModes.Ellipsis;
            playedDesc.text = "Action description";
            var playedHow = Txt(Place(NewRect("PlayedHow", played), 0, 68, 268, 68), regular, 16, new Color32(0x33, 0x42, 0x3D, 0xFF), TextAlignmentOptions.TopLeft);
            playedHow.enableAutoSizing = true;
            playedHow.fontSizeMin = 11;
            playedHow.fontSizeMax = 16;
            playedHow.overflowMode = TextOverflowModes.Ellipsis;
            playedHow.text = "How";
            if (collabContainer)
            {
                collabContainer.SetParent(played, false);
                Place((RectTransform)collabContainer, 0, 138, 268, 26);
                var grid = collabContainer.GetComponent<GridLayoutGroup>();
                if (grid)
                {
                    grid.cellSize = new Vector2(86, 26);
                    grid.spacing = new Vector2(4, 4);
                    grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    grid.constraintCount = 3;
                    grid.childAlignment = TextAnchor.UpperLeft;
                }
            }
            played.gameObject.SetActive(false);

            // ── Linked instruments ──
            var linksLabel = Txt(Place(NewRect("LinksLabel", card), 16, 248, 268, 18), bold, 14, Muted, TextAlignmentOptions.Top);
            linksLabel.text = "Linked instruments";

            var chips = Place(NewRect("Chips", card), 16, 268, 268, 92);
            chips.gameObject.AddComponent<RectMask2D>();
            var chipGrid = chips.gameObject.AddComponent<GridLayoutGroup>();
            chipGrid.cellSize = new Vector2(40, 44);
            chipGrid.spacing = new Vector2(4, 4);
            chipGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            chipGrid.childAlignment = TextAnchor.UpperCenter;

            var chip = NewRect("ChipTemplate", chips);
            var chipImg = chip.gameObject.AddComponent<Image>();
            chipImg.sprite = hex;
            chipImg.color = new Color32(0x46, 0x32, 0x7F, 0xFF); // linked instrument: logo purple
            var chipButton = chip.gameObject.AddComponent<Button>();
            chipButton.targetGraphic = chipImg;
            var chipColors = chipButton.colors;
            chipColors.disabledColor = Color.white; // read-only chips keep their colour
            chipColors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            chipButton.colors = chipColors;
            var chipText = Txt(Stretch(NewRect("Number", chip)), bold, 18, Color.white, TextAlignmentOptions.Center);
            chipText.text = "12";
            chip.gameObject.SetActive(false);

            var noLinks = Txt(Place(NewRect("NoLinks", card), 16, 268, 268, 22), regular, 14, Muted, TextAlignmentOptions.Top);
            noLinks.fontStyle = FontStyles.Italic;
            noLinks.text = "Select the card to link instruments";

            // ── Footer ──
            var line = Place(NewRect("Line", card), 16, 361, 268, 1).gameObject.AddComponent<Image>();
            line.color = Line;
            line.raycastTarget = false;

            var flip = Place(NewRect("FlipButton", card), 16, 368, 124, 38);
            var flipImg = Round(flip, ButtonLight, 10);
            flipImg.raycastTarget = true; // the button needs its image to receive clicks
            var flipButton = flip.gameObject.AddComponent<Button>();
            flipButton.targetGraphic = flipImg;
            var flipLabel = Txt(Stretch(NewRect("Label", flip)), bold, 15, Ink, TextAlignmentOptions.Center);
            flipLabel.text = "Write how";

            var select = Place(NewRect("SelectButton", card), 160, 368, 124, 38);
            var selectImg = Round(select, Orange, 10);
            selectImg.raycastTarget = true;
            var selectButton = select.gameObject.AddComponent<Button>();
            selectButton.targetGraphic = selectImg;
            var selectColors = selectButton.colors;
            selectColors.disabledColor = Color.white; // the hook colours the disabled state itself
            selectButton.colors = selectColors;
            var selectLabel = Txt(Stretch(NewRect("Label", select)), bold, 15, Color.white, TextAlignmentOptions.Center);
            selectLabel.text = "Select";

            // Collaborate button (shown by CollaborateButton to other players on played cards).
            if (collabButton)
            {
                collabButton.SetParent(card, false);
                Place((RectTransform)collabButton, 160, 368, 124, 38);
                collabButton.localScale = Vector3.one;
                var img = collabButton.GetComponent<Image>();
                if (img)
                {
                    img.sprite = round;
                    img.type = Image.Type.Sliced;
                    img.pixelsPerUnitMultiplier = SpriteRadius / 10f;
                    img.color = Collab;
                }
                foreach (var t in collabButton.GetComponentsInChildren<TMP_Text>(true))
                {
                    var trt = t.rectTransform;
                    trt.anchorMin = Vector2.zero;
                    trt.anchorMax = Vector2.one;
                    trt.offsetMin = trt.offsetMax = Vector2.zero;
                    t.font = bold;
                    t.enableAutoSizing = false;
                    t.fontSize = 15;
                    t.color = Color.white;
                    t.alignment = TextAlignmentOptions.Center;
                    t.text = "Collaborate";
                }
            }

            // ── Wire the hook ──
            var so = new SerializedObject(hook);
            void Set(string field, Object value)
            {
                var p = so.FindProperty(field);
                if (p == null) Debug.LogError($"[ActionCardBuilder] ActionCardsHook has no field '{field}'.");
                else p.objectReferenceValue = value;
            }
            Set("description", desc);
            Set("descriptionCustom", customInput);
            Set("descriptionHow", howInput);
            Set("descriptionCustomReadOnly", playedDesc);
            Set("descriptionHowReadOnly", playedHow);
            Set("frame", frame);
            Set("paper", paper);
            Set("glow", glow);
            Set("badge", badge);
            Set("numberText", number);
            Set("kindText", kind);
            Set("front", front.gameObject);
            Set("back", back.gameObject);
            Set("played", played.gameObject);
            Set("howTitle", howTitle);
            Set("linksLabel", linksLabel);
            Set("chipContainer", chips);
            Set("chipTemplate", chip.gameObject);
            Set("noLinksText", noLinks);
            Set("flipButton", flipButton);
            Set("flipLabel", flipLabel);
            Set("selectButton", selectButton);
            Set("selectFill", selectImg);
            Set("selectLabel", selectLabel);
            Set("canvasGroup", group);
            so.ApplyModifiedPropertiesWithoutUndo();

            customInput.gameObject.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // Grid tile that adds one more open card. Loaded by ActionCardsSpawner from Resources.
    private static void BuildAddTile()
    {
        var go = new GameObject("ActionCardAddTile", typeof(RectTransform));
        go.layer = 5;
        try
        {
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(W, H);

            var card = NewRect("Card", rt);
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(W, H);
            var fit = go.AddComponent<FitChildToRect>();
            fit.content = card;
            fit.referenceSize = new Vector2(W, H);

            var border = Round(Stretch(NewRect("Border", card)), new Color32(0xD8, 0xC4, 0xA8, 0xFF), 18);
            var inner = Round(Stretch(NewRect("Inner", card)), new Color(1, 1, 1, 0.6f), 15);
            inner.rectTransform.offsetMin = new Vector2(3, 3);
            inner.rectTransform.offsetMax = new Vector2(-3, -3);

            // Button on the root so the tile's whole area is clickable.
            var hit = go.AddComponent<Image>();
            hit.color = new Color(1, 1, 1, 0);
            var button = go.AddComponent<Button>();
            button.targetGraphic = border;

            var plus = Txt(Place(NewRect("Plus", card), 0, 120, W, 80), bold, 72, new Color32(0xB5, 0x65, 0x0A, 0xFF), TextAlignmentOptions.Center);
            plus.text = "+";
            var label = Txt(Place(NewRect("Label", card), 0, 210, W, 32), bold, 22, new Color32(0x2A, 0x21, 0x1A, 0xFF), TextAlignmentOptions.Center);
            label.text = "Add open card";
            // Named "Count" at the root so the spawner can find it.
            var count = Txt(Place(NewRect("Count", rt), 0, 0, 0, 0), regular, 16, new Color32(0x6B, 0x5A, 0x48, 0xFF), TextAlignmentOptions.Center);
            count.rectTransform.anchorMin = count.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            count.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            count.rectTransform.sizeDelta = new Vector2(W, 24);
            count.rectTransform.anchoredPosition = new Vector2(0, -50);
            count.text = "1 of 5 used";

            PrefabUtility.SaveAsPrefabAsset(go, AddTilePath);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    // White rounded rectangle, 9-sliced, made once into Assets/Images/ActionCard/RoundRect.png.
    private static Sprite GetOrCreateRoundRect()
    {
        string path = ArtFolder + "RoundRect.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing) return existing;

        Directory.CreateDirectory(ArtFolder);
        const int size = 96;
        const int r = SpriteRadius;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float cx = Mathf.Clamp(x + 0.5f, r, size - r);
            float cy = Mathf.Clamp(y + 0.5f, r, size - r);
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
            float a = Mathf.Clamp01(r - d + 0.5f);
            tex.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = new Vector4(r, r, r, r);
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t != parent && t.name == name) return t;
        return null;
    }

    internal static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5; // UI
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    internal static RectTransform Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    // Position in design units from the parent's top-left corner.
    internal static RectTransform Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    internal static Image Round(RectTransform rt, Color color, float radius)
    {
        var img = rt.GetComponent<Image>() ?? rt.gameObject.AddComponent<Image>();
        img.sprite = round;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = SpriteRadius / radius;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    internal static TextMeshProUGUI Txt(RectTransform rt, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
    {
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font;
        t.fontStyle = FontStyles.Normal;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.Normal;
        // Overflow, not Ellipsis: with Baloo's tall lines a label a pixel too short for its box
        // would collapse to "…". Long multi-line texts set Ellipsis themselves.
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    internal static TMP_InputField Input(string name, RectTransform rt, TMP_FontAsset font, float size, Color bg,
                                        string placeholder, int limit)
    {
        var img = Round(rt, bg, 10);
        img.raycastTarget = true;
        var outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(0xCB, 0xD5, 0xD0, 0xFF);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        var area = Stretch(NewRect("Text Area", rt));
        area.offsetMin = new Vector2(10, 8);
        area.offsetMax = new Vector2(-10, -8);
        area.gameObject.AddComponent<RectMask2D>();

        var ph = Txt(Stretch(NewRect("Placeholder", area)), font, size, new Color32(0x6E, 0x7A, 0x76, 0xFF), TextAlignmentOptions.TopLeft);
        ph.fontStyle = FontStyles.Italic;
        ph.text = placeholder;
        var text = Txt(Stretch(NewRect("Text", area)), font, size, Ink, TextAlignmentOptions.TopLeft);
        text.overflowMode = TextOverflowModes.Masking;
        text.text = "";

        var input = rt.gameObject.AddComponent<TMP_InputField>();
        input.textViewport = area;
        input.textComponent = text;
        input.placeholder = ph;
        input.targetGraphic = img;
        input.lineType = TMP_InputField.LineType.MultiLineNewline;
        input.characterLimit = limit;
        input.richText = false;
        input.fontAsset = font;
        input.pointSize = size;
        rt.gameObject.name = name;
        return input;
    }
}
