using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static ActionCardBuilder;

// Gives the board a raised-tile look: every cell is a white tile with a bevel, a light grey side and a
// soft shadow; under a placed instrument the side takes its colour (BoardTileTint). All 2D layers: the
// cells, drag and drop and tap-to-place work exactly as before.
//
// The tile layers go INSIDE each cell (BoardCell.prefab). Nothing is added among the Board's children,
// because cells are found over the network by their sibling index.
//
// Cells overlap a little (164 wide, 149 apart), so the tile is drawn smaller than its cell to leave a
// gap; placed pieces take the tile's size (DragPiece.MatchCellSize).
public static class BoardBuilder
{
    private const string ScenePath = "Assets/Scenes/MainGame.unity";
    private const string CellPrefabPath = "Assets/Prefabs/BoardCell.prefab";
    private const string ArtFolder = "Assets/Images/Board/";

    private const float TileScale = 0.87f;    // tile size / cell size
    private const float SideDepth = 9f;       // how far the tile's side shows under it
    private const float ShadowDrop = 15f;

    // Whites and cool light greys.
    private static readonly Color FaceTop = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
    private static readonly Color FaceBottom = new Color32(0xF1, 0xEF, 0xF6, 0xFF);
    private static readonly Color BevelLight = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
    private static readonly Color BevelDark = new Color32(0xDD, 0xDA, 0xE6, 0xFF);
    private static readonly Color TileSide = new Color32(0xCF, 0xCB, 0xDA, 0xFF);
    private static readonly Color TileShadow = new Color(0.08f, 0.05f, 0.18f, 0.22f);

    [MenuItem("BioValue/Build Board")]
    public static void Build()
    {
        if (!LoadArt())
        {
            EditorUtility.DisplayDialog("Build Board", "Missing art. Run BioValue/Build Instrument Piece first.", "OK");
            return;
        }
        if (!AssetDatabase.IsValidFolder("Assets/Images/Board")) AssetDatabase.CreateFolder("Assets/Images", "Board");

        // Big enough that nothing is stretched up on screen (that is what made the edges jagged).
        var tileTop = CreateHex("TileTop.png", false, 512, 592, true, TileTopColor);
        var tileFlat = CreateHex("TileFlat.png", false, 512, 592, true, (d, ny, fy) => Color.white);

        BuildCell(tileTop, tileFlat);

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        RemoveSlab(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[BoardBuilder] Board tiles built (BoardCell.prefab and MainGame saved).");
    }

    // ── Cell: Shadow, Side, Top (back to front) inside the cell ──

    private static void BuildCell(Sprite tileTop, Sprite tileFlat)
    {
        var root = PrefabUtility.LoadPrefabContents(CellPrefabPath);
        try
        {
            foreach (var name in new[] { "Shadow", "Side", "Top" })
            {
                Transform old;
                while ((old = root.transform.Find(name)) != null) Object.DestroyImmediate(old.gameObject);
            }

            // The cell's own image stays as an invisible hexagon (tap-to-place copies its shape).
            var cellImage = root.GetComponent<Image>();
            if (cellImage)
            {
                cellImage.sprite = hex;
                cellImage.color = new Color(1, 1, 1, 0);
                cellImage.preserveAspect = false;
            }

            var size = ((RectTransform)root.transform).sizeDelta;
            var inset = size * (1f - TileScale) * 0.5f;
            Layer(root.transform, "Shadow", tileFlat, TileShadow, inset - new Vector2(2, 2), ShadowDrop);
            var side = Layer(root.transform, "Side", tileFlat, TileSide, inset, SideDepth);
            Layer(root.transform, "Top", tileTop, Color.white, inset, 0f);

            var tint = root.GetComponent<BoardTileTint>() ?? root.AddComponent<BoardTileTint>();
            tint.side = side;

            PrefabUtility.SaveAsPrefabAsset(root, CellPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Image Layer(Transform parent, string name, Sprite sprite, Color color, Vector2 inset, float drop)
    {
        var rt = Stretch(NewRect(name, parent));
        rt.offsetMin = new Vector2(inset.x, inset.y - drop);
        rt.offsetMax = new Vector2(-inset.x, -inset.y - drop);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // An earlier version put a frosted slab behind the board ("BoardBase", next to "Board"); remove it.
    private static void RemoveSlab(UnityEngine.SceneManagement.Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<RectTransform>(true))
                if (t && t.name == "BoardBase" && t.parent && t.parent.Find("Board"))
                    Object.DestroyImmediate(t.gameObject);
        foreach (var file in new[] { "SlabTop.png", "SlabFlat.png" })
            AssetDatabase.DeleteAsset(ArtFolder + file);
    }

    // ── Generated hexagon sprites ──

    // d: distance into the shape from the edge (texture px), ny: which way the nearest edge faces
    // (+1 up, -1 down), fy: height in the shape (0 bottom, 1 top).
    private delegate Color Shade(float d, float ny, float fy);

    private static Color TileTopColor(float d, float ny, float fy)
    {
        const float bevel = 18f; // ~5 px on screen
        var face = Color.Lerp(FaceBottom, FaceTop, Mathf.SmoothStep(0f, 1f, fy));
        if (d >= bevel) return face;
        var edge = Color.Lerp(BevelDark, BevelLight, (ny + 1f) * 0.5f);
        return Color.Lerp(edge, face, Mathf.SmoothStep(0f, 1f, d / bevel));
    }

    // Draws a regular hexagon (pointy-top, or flat-top) into a PNG and imports it as a sprite.
    // Always redrawn, so changes to the colours above show up on the next build.
    private static Sprite CreateHex(string file, bool flatTop, int w, int h, bool mipmaps, Shade shade)
    {
        string path = ArtFolder + file;
        const float sin60 = 0.8660254f;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color32[w * h];
        float cx = w * 0.5f, cy = h * 0.5f;
        // Apothem across the flat sides; leave 2px for anti-aliasing.
        float apothem = (flatTop ? h : w) * 0.5f - 2f;
        float circum = apothem / sin60;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float px = x + 0.5f - cx, py = y + 0.5f - cy;
            // Work in a pointy-top frame; a flat-top hexagon is the same one turned 90°.
            float u = flatTop ? py : px, v = flatTop ? px : py;
            float e1 = Mathf.Abs(u);
            float e2 = 0.5f * u + sin60 * v, e3 = 0.5f * u - sin60 * v;
            float m = Mathf.Max(e1, Mathf.Abs(e2), Mathf.Abs(e3));
            float d = apothem - m; // > 0 inside
            float alpha = Mathf.Clamp01(d + 0.5f);
            if (alpha <= 0f) { pixels[y * w + x] = new Color32(255, 255, 255, 0); continue; }

            // Normal of the nearest edge, back in texture space; its y says up/down facing.
            Vector2 n;
            if (m == e1) n = new Vector2(Mathf.Sign(u), 0f);
            else if (m == Mathf.Abs(e2)) n = new Vector2(0.5f, sin60) * Mathf.Sign(e2);
            else n = new Vector2(0.5f, -sin60) * Mathf.Sign(e3);
            float ny = flatTop ? n.x : n.y;

            float extent = flatTop ? apothem : circum;
            float fy = Mathf.Clamp01((py + extent) / (2f * extent));
            var c = shade(d, ny, fy);
            c.a *= alpha;
            pixels[y * w + x] = c;
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = mipmaps;     // tiles are drawn ~3.5x smaller than the texture
        importer.filterMode = FilterMode.Trilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
