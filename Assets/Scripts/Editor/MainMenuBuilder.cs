using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static ActionCardBuilder;

// Main menu (option A of the mockup): the logo's purple, floating hexes in pink/blue/orange like board cells,
// the official horizontal logo (coloured symbol + white wordmark) in the centre, a short line,
// a big pink "Start game" and a quiet "Quit". Only the hexes move.
// The old menu objects under the Canvas are switched off, not deleted.
public static class MainMenuBuilder
{
    private const string ScenePath = "Assets/Scenes/Main Menu.unity";
    private const string ArtFolder = "Assets/Images/MainMenu/";
    private const string SymbolSource = "Assets/Images/UI/BIV_logo_SYMBOL.png"; // official, imported as Multiple
    private const string SymbolPath = ArtFolder + "BIV_logo_SYMBOL.png";        // same file, as one sprite
    private const string WordmarkPath = ArtFolder + "BIV_wordmark_WHT.png";      // cut from BIV_logo_LOGO_HOR WHT
    private const string RootName = "MainMenuUI";

    // The official horizontal logo is 5318 × 1596: symbol 1410 wide, wordmark from x 1412 (3906 wide).
    private const float LogoW = 5318f, LogoH = 1596f, SymbolW = 1410f, WordmarkX = 1412f, WordmarkW = 3906f;

    // The mockup is 1440 wide, the canvas 1920.
    private const float K = 1920f / 1440f;

    private static readonly Color Pink = new Color32(0xFE, 0x49, 0xA4, 0xFF);
    private static readonly Color Blue = new Color32(0x25, 0x7A, 0xFE, 0xFF);
    private static readonly Color Orange = new Color32(0xFD, 0xAA, 0x12, 0xFF);

    [MenuItem("BioValue/Build Main Menu")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Build Main Menu", "Stop Play mode first: the builder edits the scene.", "OK");
            return;
        }
        if (!LoadArt())
        {
            EditorUtility.DisplayDialog("Build Main Menu",
                "Missing art or font. Run BioValue/Build Instrument Piece and BioValue/Build Action Card first.", "OK");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath);
        }

        Canvas canvas = null;
        foreach (var go in scene.GetRootGameObjects())
            if (go.name == "Canvas" && go.TryGetComponent(out canvas)) break;
        var manager = Object.FindFirstObjectByType<MainMenuManager>(FindObjectsInactive.Include);
        if (!canvas || !manager)
        {
            EditorUtility.DisplayDialog("Build Main Menu", "Couldn't find the Canvas or the MainMenuManager in the Main Menu scene.", "OK");
            return;
        }
        var fading = Object.FindFirstObjectByType<FadingScript>(FindObjectsInactive.Include);

        var canvasT = (RectTransform)canvas.transform;
        var old = canvasT.Find(RootName);
        if (old) Object.DestroyImmediate(old.gameObject);
        foreach (Transform child in canvasT) child.gameObject.SetActive(false); // the old menu

        var root = Stretch(NewRect(RootName, canvasT));
        root.SetAsFirstSibling();

        // Background: radial purple, lighter a bit above the middle.
        var bg = Stretch(NewRect("Background", root)).gameObject.AddComponent<Image>();
        bg.sprite = Gradient();
        bg.raycastTarget = false;

        // Floating hexes (x, y, width in mockup units; colour, opacity, rotation, delay).
        var hexes = Stretch(NewRect("Hexes", root));
        Hex(hexes, 140, 120, 110, Pink, .9f, -12, 0f);
        Hex(hexes, 250, 560, 70, Orange, .9f, 8, 1.2f);
        Hex(hexes, 80, 400, 44, Blue, .8f, 0, 2.1f);
        Hex(hexes, 1180, 150, 90, Blue, .9f, 14, .6f);
        Hex(hexes, 1250, 520, 120, Orange, .85f, -6, 1.8f);
        Hex(hexes, 1110, 640, 50, Pink, .9f, 10, 2.6f);
        Hex(hexes, 1340, 360, 40, Color.white, .25f, 0, 3f);

        // Official horizontal logo.
        float logoW = 680 * K, logoH = logoW * LogoH / LogoW;
        var logo = Top(NewRect("Logo", root), 215 * K, logoW, logoH);
        LogoPart(logo, "Symbol", Single(SymbolPath, SymbolSource), 0, SymbolW / LogoW);
        LogoPart(logo, "Wordmark", Single(WordmarkPath, null), WordmarkX / LogoW, WordmarkW / LogoW);

        float y = 215 * K + logoH + 22 * K;
        var tagline = Txt(Top(NewRect("Tagline", root), y, 900, 42), medium, 22 * K,
                          new Color(1, 1, 1, .75f), TextAlignmentOptions.Center);
        tagline.text = "Plan together · Vote · Collaborate";
        tagline.characterSpacing = 2f;

        // Start game: pink pill with a round arrow on the right.
        y += 42 + 42 * K;
        float startH = 64 * K;
        var start = Top(NewRect("StartButton", root), y, 300, startH);
        var startFill = Round(start, Pink, startH / 2);
        startFill.raycastTarget = true;
        var startButton = start.gameObject.AddComponent<Button>();
        startButton.targetGraphic = startFill;
        Tints(startButton);
        var startLabel = Txt(NewRect("Label", start), bold, 24 * K, Color.white, TextAlignmentOptions.MidlineLeft);
        var labelRt = (RectTransform)startLabel.transform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(34 * K, 0);
        labelRt.offsetMax = new Vector2(-(12 + 36 + 8) * K, 0);
        startLabel.text = "Start game";
        startLabel.textWrappingMode = TextWrappingModes.NoWrap;
        var circle = NewRect("Arrow", start);
        circle.anchorMin = circle.anchorMax = circle.pivot = new Vector2(1, 0.5f);
        circle.anchoredPosition = new Vector2(-12 * K, 0);
        circle.sizeDelta = new Vector2(36 * K, 36 * K);
        Round(circle, new Color(1, 1, 1, .25f), 18 * K);
        var chevron = NewRect("Chevron", circle);
        chevron.anchorMin = chevron.anchorMax = chevron.pivot = new Vector2(0.5f, 0.5f);
        chevron.sizeDelta = new Vector2(26, 26);
        var chevronImg = chevron.gameObject.AddComponent<Image>();
        chevronImg.sprite = ActionDrawerBuilder.GetOrCreateChevron();
        chevronImg.color = Color.white;
        chevronImg.raycastTarget = false;
        UnityEventTools.AddPersistentListener(startButton.onClick, manager.StartGame);
        if (fading) UnityEventTools.AddPersistentListener(startButton.onClick, fading.FadeIn);

        // Quit: outlined pill.
        y += startH + 14 * K;
        float quitH = 48 * K;
        var quit = Top(NewRect("QuitButton", root), y, 140, quitH);
        var hit = quit.gameObject.AddComponent<Image>();
        hit.color = Color.clear; // clickable everywhere, not only on the outline
        var ring = Stretch(NewRect("Outline", quit)).gameObject.AddComponent<Image>();
        ring.sprite = Ring();
        ring.type = Image.Type.Sliced;
        ring.pixelsPerUnitMultiplier = 24f / (quitH / 2);
        ring.color = new Color(1, 1, 1, .35f);
        ring.raycastTarget = false;
        var quitButton = quit.gameObject.AddComponent<Button>();
        quitButton.targetGraphic = ring;
        Tints(quitButton);
        var quitLabel = Txt(Stretch(NewRect("Label", quit)), bold, 19 * K, Color.white, TextAlignmentOptions.Center);
        quitLabel.text = "Quit";
        UnityEventTools.AddPersistentListener(quitButton.onClick, manager.QuitGame);

        // Version, bottom right.
        var version = NewRect("Version", root);
        version.anchorMin = version.anchorMax = version.pivot = new Vector2(1, 0);
        version.anchoredPosition = new Vector2(-24 * K, 16 * K);
        version.sizeDelta = new Vector2(200, 28);
        var versionLabel = Txt(version, regular, 14 * K, new Color(1, 1, 1, .4f), TextAlignmentOptions.BottomRight);
        versionLabel.text = "v" + PlayerSettings.bundleVersion;

        manager.startButton = startButton;
        manager.startFill = startFill;
        manager.startLabel = startLabel;
        manager.versionLabel = versionLabel;
        EditorUtility.SetDirty(manager);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MainMenuBuilder] Main menu built (the old menu objects are switched off under the Canvas).");
    }

    // Centred horizontally, `top` units from the top of the screen.
    private static RectTransform Top(RectTransform rt, float top, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -top);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    private static void LogoPart(RectTransform logo, string name, Sprite sprite, float x, float w)
    {
        var rt = NewRect(name, logo);
        rt.anchorMin = new Vector2(x, 0);
        rt.anchorMax = new Vector2(x + w, 1);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
    }

    // Hexes on the left half stay with the left edge, the others with the right edge (wider screens).
    private static void Hex(RectTransform parent, float x, float y, float w, Color color, float alpha, float rotation, float delay)
    {
        float width = w * K, height = w * 1.15f * K;
        float cx = x * K + width / 2, cy = y * K + height / 2;
        bool left = cx < 960;
        var rt = NewRect("Hex", parent);
        rt.anchorMin = rt.anchorMax = new Vector2(left ? 0 : 1, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(left ? cx : cx - 1920, -cy);
        rt.sizeDelta = new Vector2(width, height);
        rt.localEulerAngles = new Vector3(0, 0, -rotation); // CSS turns clockwise
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = hex;
        color.a = alpha;
        img.color = color;
        img.raycastTarget = false;
        var drift = rt.gameObject.AddComponent<MenuHexDrift>();
        drift.height = 14 * K;
        drift.delay = delay;
    }

    private static void Tints(Button button)
    {
        var colors = button.colors;
        colors.normalColor = colors.selectedColor = colors.disabledColor = Color.white;
        colors.highlightedColor = new Color(0.93f, 0.93f, 0.93f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        button.colors = colors;
    }

    // A copy of `source` (when given and missing) imported as one sprite.
    private static Sprite Single(string path, string source)
    {
        if (source != null && !File.Exists(path)) AssetDatabase.CopyAsset(source, path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single
                         || !importer.alphaIsTransparency))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true; // drawn much smaller than the file
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // radial-gradient(120% 90% at 50% 40%, #5434C8 0%, #3F27A0 45%, #241A5E 100%)
    private static Sprite Gradient()
    {
        string path = ArtFolder + "MenuBackground.png";
        if (!File.Exists(path))
        {
            Color a = new Color32(0x54, 0x34, 0xC8, 0xFF), b = new Color32(0x3F, 0x27, 0xA0, 0xFF), c = new Color32(0x24, 0x1A, 0x5E, 0xFF);
            const int w = 480, h = 270;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int py = 0; py < h; py++)
            for (int px = 0; px < w; px++)
            {
                float dx = (px + 0.5f) / w - 0.5f, dy = (h - py - 0.5f) / h - 0.4f; // texture rows go up
                float t = Mathf.Sqrt(dx / 1.2f * (dx / 1.2f) + dy / 0.9f * (dy / 0.9f));
                tex.SetPixel(px, py, t < 0.45f ? Color.Lerp(a, b, t / 0.45f) : Color.Lerp(b, c, Mathf.Clamp01((t - 0.45f) / 0.55f)));
            }
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
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // White rounded-rectangle outline (2 px at radius 24), 9-sliced.
    private static Sprite Ring()
    {
        string path = ArtFolder + "RoundRing.png";
        if (!File.Exists(path))
        {
            const int size = 96, r = 24;
            const float stroke = 2f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, r, size - r);
                float cy = Mathf.Clamp(y + 0.5f, r, size - r);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                float fromEdge = r - d; // distance from the outer edge, inwards
                float alpha = Mathf.Clamp01(fromEdge + 0.5f) * Mathf.Clamp01(stroke - fromEdge + 0.5f);
                tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer.spriteImportMode != SpriteImportMode.Single || importer.spriteBorder.x < 1)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(24, 24, 24, 24);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
