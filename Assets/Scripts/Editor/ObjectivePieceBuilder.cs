using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static ActionCardBuilder;

// Rebuilds Objective.prefab as the iridescent tile of the mockup "Objective piece": the pastel gradient
// face (Objective_back.png) inside a rainbow rim, its side the same rainbow but darker, a
// soft black shadow, the BioValue symbol, "GAME OBJECTIVE" and dark text; behind it a breathing gold
// aura, and a shine sweeping across the face (ObjectivePieceFx).
// Rebuilt inside the existing prefab: its root keeps the PhotonView, drag (switched off), click-for-details and
// ObjectiveHook (pointed at the new text). Run "BioValue/Build Objective Piece".
public static class ObjectivePieceBuilder
{
    private const string PrefabPath = "Assets/Resources/PhotonPrefabs/Objective.prefab";
    private const string ArtFolder = "Assets/Images/Objective/";
    private const string BoardArt = "Assets/Images/Board/";
    private const string PastelPath = "Assets/Images/Objective_back.png";
    private const string SymbolPath = "Assets/Images/MainMenu/BIV_logo_SYMBOL.png";

    // The board's tile: cell 164 wide, tile drawn at 0.87 (BoardBuilder.TileScale).
    private const float TileW = 143f, TileH = 165f, Side = 14f;

    private static readonly Color Ink = new Color32(0x24, 0x1B, 0x3A, 0xFF);
    private static readonly Color SideTint = new Color(0.55f, 0.5f, 0.62f, 1f);   // the rainbow rim, darker
    private static readonly Color Gold = new Color(1f, 0.80f, 0.35f, 0.8f);

    [MenuItem("BioValue/Build Objective Piece")]
    public static void Build()
    {
        if (!LoadArt())
        {
            EditorUtility.DisplayDialog("Build Objective Piece", "Missing art or font. Run BioValue/Build Action Card first.", "OK");
            return;
        }
        Directory.CreateDirectory(ArtFolder);
        var flat = AssetDatabase.LoadAssetAtPath<Sprite>(BoardArt + "TileFlat.png") ?? hex;
        var top = AssetDatabase.LoadAssetAtPath<Sprite>(BoardArt + "TileTop.png") ?? hex;
        var glow = Generated("SoftGlow.png", 128, 128, (x, y, s) =>
        {
            float d = Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), new Vector2(s / 2f, s / 2f)) / (s / 2f));
            float a = Mathf.SmoothStep(1f, 0f, d);
            return new Color(1, 1, 1, a * a);
        });
        var band = Generated("Sheen.png", 32, 4, (x, y, s) =>
            new Color(1, 1, 1, Mathf.Sin(Mathf.PI * x / s)));
        Sprite pastel = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(PastelPath))
            if (a is Sprite sp) { pastel = sp; break; }
        if (!pastel) pastel = top;
        var symbol = AssetDatabase.LoadAssetAtPath<Sprite>(SymbolPath);
        var rainbow = Rainbow();

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        var rootRt = (RectTransform)root.transform;
        // The old look ("Container") or the last build ("Piece").
        for (int i = rootRt.childCount - 1; i >= 0; i--)
        {
            var c = rootRt.GetChild(i);
            if (c.name == "Container" || c.name == "Piece") Object.DestroyImmediate(c.gameObject);
        }

        // The root keeps its Image as the (invisible) hit area: a click opens the details.
        var hit = root.GetComponent<Image>();
        if (hit)
        {
            hit.sprite = flat;
            hit.color = new Color(1, 1, 1, 0);
            hit.raycastTarget = true;
            hit.preserveAspect = true;
        }

        // It stays in the middle of the board: no dragging, a click opens the details.
        var drag = root.GetComponent<DragPiece>();
        if (drag) drag.enabled = false;

        var piece = Center(NewRect("Piece", rootRt), 0, Side / 2f, TileW, TileH);
        Layer(piece, "Aura", glow, Gold, 0, -Side / 2f, 280, 280);
        Layer(piece, "Shadow", flat, new Color(0f, 0f, 0f, 0.4f), 0, -Side - 8, TileW - 6, TileH - 4);
        Layer(piece, "Side", rainbow, SideTint, 0, -Side, TileW, TileH);
        Layer(piece, "Rim", rainbow, Color.white, 0, 0, TileW, TileH);
        var face = Layer(piece, "Face", pastel, Color.white, 0, 0, TileW - 14, TileH - 16);
        face.gameObject.AddComponent<Mask>().showMaskGraphic = true; // the sheen stays inside the hexagon

        var sheen = Layer(face.rectTransform, "Sheen", band, new Color(1, 1, 1, 0.85f), -190, 0, 46, 260);
        sheen.rectTransform.localRotation = Quaternion.Euler(0, 0, -20);

        if (symbol)
        {
            var logo = Layer(face.rectTransform, "Symbol", symbol, Color.white, 0, 54, 28, 28);
            logo.preserveAspect = true;
        }
        var text = Txt(Center(NewRect("Text", face.rectTransform), 0, -2, 104, 84), bold, 22, Ink, TextAlignmentOptions.Center); // under the symbol, no label: bigger text
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.enableAutoSizing = true;
        text.fontSizeMin = 13;
        text.fontSizeMax = 22;
        text.lineSpacing = -18;
        text.raycastTarget = false;
        text.text = "Bring nature back to the river banks";

        var fx = root.GetComponent<ObjectivePieceFx>();
        if (!fx) fx = root.AddComponent<ObjectivePieceFx>();
        fx.piece = piece;
        fx.aura = piece.Find("Aura").GetComponent<Image>();
        fx.sheen = sheen.rectTransform;
        fx.flag = null; // no flag on the iridescent tile
        fx.tileWidth = TileW;

        var hook = root.GetComponent<ObjectiveHook>();
        if (hook)
        {
            var so = new SerializedObject(hook);
            so.FindProperty("objectiveText").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        BuildDetails(rainbow);
        Debug.Log("[ObjectivePieceBuilder] " + PrefabPath + " and " + DetailsPath + " rebuilt.");
    }

    // ── Details (click the piece): the objective in full and the link to the materials ──

    private const string DetailsPath = "Assets/Prefabs/Objective_Details.prefab";
    private static readonly Color EyebrowPurple = new Color32(0x6A, 0x55, 0xA8, 0xFF);
    private static readonly Color Muted = new Color32(0x6B, 0x65, 0x84, 0xFF);
    private static readonly Color Faint = new Color32(0xA9, 0xA3, 0xBC, 0xFF);
    private static readonly Color Light = new Color32(0xF1, 0xEF, 0xF6, 0xFF);
    private static readonly Color PinkButton = new Color32(0xFE, 0x49, 0xA4, 0xFF);

    private static void BuildDetails(Sprite icon)
    {
        var root = PrefabUtility.LoadPrefabContents(DetailsPath);
        var rt = (RectTransform)root.transform;
        for (int i = rt.childCount - 1; i >= 0; i--) Object.DestroyImmediate(rt.GetChild(i).gameObject);
        var old = root.GetComponents<Component>();
        for (int i = old.Length - 1; i >= 0; i--) // last first: an Image needs its CanvasRenderer
            if (!(old[i] is Transform) && !(old[i] is ObjectiveDetails) && !(old[i] is Photon.Pun.PhotonView))
                Object.DestroyImmediate(old[i]);
        Stretch(rt);
        rt.localScale = Vector3.one;
        var details = root.GetComponent<ObjectiveDetails>();
        if (!details) details = root.AddComponent<ObjectiveDetails>();

        // Dim backdrop: a click outside the card closes it.
        var backdrop = Stretch(NewRect("Backdrop", rt)).gameObject.AddComponent<Image>();
        backdrop.color = new Color(0.08f, 0.05f, 0.18f, 0.5f);
        var backdropButton = backdrop.gameObject.AddComponent<Button>();
        backdropButton.transition = Selectable.Transition.None;

        const float w = 747, h = 450, pad = 40;
        var card = Center(NewRect("Card", rt), 0, 0, w, h);
        var cardImg = Round(card, Color.white, 35);
        cardImg.raycastTarget = true;
        var shadow = card.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.04f, 0.02f, 0.16f, 0.35f);
        shadow.effectDistance = new Vector2(0, -10);

        var hexRt = Place(NewRect("Icon", card), pad, 36, 52, 60);
        var hexImg = hexRt.gameObject.AddComponent<Image>();
        hexImg.sprite = icon ? icon : hex;
        hexImg.raycastTarget = false;
        var eyebrow = Txt(Place(NewRect("Eyebrow", card), 108, 36, 480, 60), bold, 20, EyebrowPurple, TextAlignmentOptions.MidlineLeft);
        eyebrow.text = "GAME OBJECTIVE";
        eyebrow.characterSpacing = 8;

        var close = Place(NewRect("Close", card), w - pad - 54, 39, 54, 54);
        var closeImg = Round(close, Light, 27);
        closeImg.raycastTarget = true;
        var closeButton = close.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImg;
        MainMenuBuilder.Tints(closeButton);
        foreach (float angle in new[] { 45f, -45f })
        {
            var bar = Center(NewRect("Bar", close), 0, 0, 24, 4.5f);
            bar.localRotation = Quaternion.Euler(0, 0, angle);
            Round(bar, Ink, 2.25f).raycastTarget = false;
        }

        var title = Txt(Place(NewRect("Objective", card), pad, 116, w - 2 * pad, 150), bold, 36, Ink, TextAlignmentOptions.TopLeft);
        title.textWrappingMode = TextWrappingModes.Normal;
        title.overflowMode = TextOverflowModes.Ellipsis;
        title.enableAutoSizing = true;
        title.fontSizeMin = 24;
        title.fontSizeMax = 36;
        title.lineSpacing = -10;
        title.text = "Bring nature back to the river banks of the city centre";
        var sub = Txt(Place(NewRect("Sub", card), pad, 272, w - 2 * pad, 30), regular, 21, Muted, TextAlignmentOptions.TopLeft);
        sub.text = "Every player works towards it, each with their own personal objective too.";

        var materials = LobbyBuilder.PillButton(Place(NewRect("Materials", card), pad, 322, 360, 67), PinkButton, "Open the materials", 25, Color.white);
        var label = (RectTransform)materials.transform.Find("Label");
        label.offsetMax = new Vector2(-22, 0);
        var arrow = NewRect("Arrow", materials.transform);
        arrow.anchorMin = arrow.anchorMax = new Vector2(1, 0.5f);
        arrow.anchoredPosition = new Vector2(-34, 0);
        arrow.sizeDelta = new Vector2(18, 18);
        var arrowImg = arrow.gameObject.AddComponent<Image>();
        arrowImg.sprite = ActionDrawerBuilder.GetOrCreateChevron();
        arrowImg.color = Color.white;
        arrowImg.raycastTarget = false;
        var back = LobbyBuilder.ChipButton(Place(NewRect("Back", card), pad + 360 + 16, 322, w - 2 * pad - 360 - 16, 67), "Back to the board", 24, 33.5f);

        var link = Txt(Place(NewRect("Link", card), pad, 404, w - 2 * pad, 30), regular, 18, Faint, TextAlignmentOptions.TopLeft);
        link.textWrappingMode = TextWrappingModes.NoWrap;
        link.overflowMode = TextOverflowModes.Ellipsis;
        link.text = "lisbon.pt/workshop-materials";

        var so = new SerializedObject(details);
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("linkText").objectReferenceValue = link;
        so.FindProperty("linkButton").objectReferenceValue = materials;
        so.FindProperty("card").objectReferenceValue = card;
        var closes = so.FindProperty("closeButtons");
        closes.arraySize = 3;
        closes.GetArrayElementAtIndex(0).objectReferenceValue = backdropButton;
        closes.GetArrayElementAtIndex(1).objectReferenceValue = closeButton;
        closes.GetArrayElementAtIndex(2).objectReferenceValue = back;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, DetailsPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // Hexagon (pointy top) in saturated iridescent colours running diagonally: pink, orange, green,
    // cyan, blue, violet. The rim, and darker the side.
    private static Sprite Rainbow()
    {
        Color[] stops =
        {
            new Color32(0xFE, 0x49, 0xA4, 0xFF), new Color32(0xFD, 0xAA, 0x12, 0xFF), new Color32(0x7B, 0xD8, 0x8F, 0xFF),
            new Color32(0x45, 0xC3, 0xE8, 0xFF), new Color32(0x25, 0x7A, 0xFE, 0xFF), new Color32(0xB0, 0x7C, 0xFF, 0xFF),
        };
        const int w = 256, h = 296;
        return Generated("RainbowHex.png", w, h, (x, y, _) =>
        {
            // inside the hexagon (vertex radius r, half-width r·√3/2), soft 1.5 px edge
            float r = h / 2f, cx = x - w / 2f, cy = y - h / 2f;
            float ax = Mathf.Abs(cx), ay = Mathf.Abs(cy);
            float halfW = r * 0.8660254f;
            float inside = Mathf.Min(halfW - ax, (r - ay) * 0.8660254f - ax * 0.5f);
            float alpha = Mathf.Clamp01(inside / 1.5f + 0.5f);
            // bottom-left → top-right
            float t = Mathf.Clamp01((x / w) * 0.6f + (y / (float)h) * 0.4f) * (stops.Length - 1);
            int i = Mathf.Min((int)t, stops.Length - 2);
            var c = Color.Lerp(stops[i], stops[i + 1], t - i);
            c = Color.Lerp(c, Color.white, 0.12f);
            c.a = alpha;
            return c;
        });
    }

    // Centred in the parent, offset by (x, y).
    private static RectTransform Center(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    private static Image Layer(RectTransform parent, string name, Sprite sprite, Color color, float x, float y, float w, float h)
    {
        var img = Center(NewRect(name, parent), x, y, w, h).gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private delegate Color PixelAt(float x, float y, int size);

    private static Sprite Generated(string name, int w, int h, PixelAt pixel)
    {
        string path = ArtFolder + name;
        if (!File.Exists(path))
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, pixel(x + 0.5f, y + 0.5f, w));
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
