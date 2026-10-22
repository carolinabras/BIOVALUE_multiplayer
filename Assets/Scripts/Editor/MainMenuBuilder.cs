using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static ActionCardBuilder;

// Main menu and Loading scene (mockups "Main menu" option A and "Start game → Loading → Lobby"):
//  - Both: the logo's purple, floating hexes in pink/blue/orange like board cells, the official horizontal
//    logo (coloured symbol + white wordmark). Same places in both scenes, so the scene change can't be seen.
//  - Main menu: a short line, a big pink "Start game" (once pressed: "Connecting…" with a spinner, Quit dims)
//    and a quiet "Quit". Only the hexes move.
//  - Loading: the logo glides up, three board cells take turns, the step (Server · Lobby · Ready), Cancel;
//    if the connection fails, "Try again" / "Back to menu".
// The old objects under each Canvas are switched off, not deleted.
public static class MainMenuBuilder
{
    private const string MenuScenePath = "Assets/Scenes/Main Menu.unity";
    private const string LoadingScenePath = "Assets/Scenes/Loading.unity";
    private const string ArtFolder = "Assets/Images/MainMenu/";
    private const string SymbolSource = "Assets/Images/UI/BIV_logo_SYMBOL.png"; // official, imported as Multiple
    private const string SymbolPath = ArtFolder + "BIV_logo_SYMBOL.png";        // same file, as one sprite
    private const string WordmarkPath = ArtFolder + "BIV_wordmark_WHT.png";      // cut from BIV_logo_LOGO_HOR WHT
    private const string MenuRootName = "MainMenuUI";
    private const string LoadingRootName = "LoadingUI";

    // The official horizontal logo is 5318 × 1596: symbol 1410 wide, wordmark from x 1412 (3906 wide).
    private const float LogoW = 5318f, LogoH = 1596f, SymbolW = 1410f, WordmarkX = 1412f, WordmarkW = 3906f;

    // The mockup is 1440 wide, the canvas 1920.
    private const float K = 1920f / 1440f;

    // Logo: width and top on the menu, and on the loading screen.
    private const float MenuLogoTop = 215 * K, MenuLogoWidth = 680 * K;
    private const float LoadingLogoTop = 130 * K, LoadingLogoWidth = 440 * K;

    private static readonly Color Pink = new Color32(0xFE, 0x49, 0xA4, 0xFF);
    private static readonly Color Blue = new Color32(0x25, 0x7A, 0xFE, 0xFF);
    private static readonly Color Orange = new Color32(0xFD, 0xAA, 0x12, 0xFF);

    // ── Main menu ────────────────────────────────────────────────────────────

    [MenuItem("BioValue/Build Main Menu")]
    public static void BuildMenu()
    {
        if (!Ready("Build Main Menu") || !Open(MenuScenePath, out var scene)) return;

        var canvas = FindCanvas(scene);
        var manager = Object.FindFirstObjectByType<MainMenuManager>(FindObjectsInactive.Include);
        if (!canvas || !manager)
        {
            EditorUtility.DisplayDialog("Build Main Menu", "Couldn't find the Canvas or the MainMenuManager in the Main Menu scene.", "OK");
            return;
        }

        var root = NewRoot(canvas, MenuRootName);
        Backdrop(root);
        var logo = Logo(root);
        Top(logo, MenuLogoTop, MenuLogoWidth, MenuLogoWidth * LogoH / LogoW);

        float y = MenuLogoTop + MenuLogoWidth * LogoH / LogoW + 22 * K;
        var tagline = Txt(Top(NewRect("Tagline", root), y, 900, 42), medium, 22 * K,
                          new Color(1, 1, 1, .75f), TextAlignmentOptions.Center);
        tagline.text = "Plan together · Vote · Collaborate";
        tagline.characterSpacing = 2f;

        // Start game: pink pill with a round arrow on the right (a spinner once pressed).
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
        var chevron = Centered(NewRect("Chevron", circle), 26);
        var chevronImg = chevron.gameObject.AddComponent<Image>();
        chevronImg.sprite = ActionDrawerBuilder.GetOrCreateChevron();
        chevronImg.color = Color.white;
        chevronImg.raycastTarget = false;
        var spinner = Centered(NewRect("Spinner", circle), 18 * K);
        var spinnerImg = spinner.gameObject.AddComponent<Image>();
        spinnerImg.sprite = Spinner();
        spinnerImg.raycastTarget = false;
        spinner.gameObject.AddComponent<MenuSpin>();
        spinner.gameObject.SetActive(false);
        UnityEventTools.AddPersistentListener(startButton.onClick, manager.StartGame);

        // How to play: outlined pill.
        y += startH + 14 * K;
        float quitH = 48 * K;
        var help = Top(NewRect("HelpButton", root), y, 220, quitH);
        var helpButton = OutlinedButton(help, "How to play", 19 * K, quitH);
        UnityEventTools.AddPersistentListener(helpButton.onClick, manager.OpenHelp);

        // Quit: outlined pill.
        y += quitH + 12 * K;
        var quit = Top(NewRect("QuitButton", root), y, 140, quitH);
        var quitButton = OutlinedButton(quit, "Quit", 19 * K, quitH);
        var quitGroup = quit.gameObject.AddComponent<CanvasGroup>();
        UnityEventTools.AddPersistentListener(quitButton.onClick, manager.QuitGame);

        var versionLabel = Version(root);

        // Rejoin (only while this device can go back to a game it dropped out of): a translucent pill at
        // the top, "● Your game ROOM is still on" and a blue "Rejoin" button.
        float bannerH = 56 * K;
        var banner = Top(NewRect("RejoinBanner", root), 40 * K, 820, bannerH);
        Round(banner, new Color(1, 1, 1, .14f), bannerH / 2);
        var dot = NewRect("Dot", banner);
        dot.anchorMin = dot.anchorMax = dot.pivot = new Vector2(0, 0.5f);
        dot.anchoredPosition = new Vector2(26 * K, 0);
        dot.sizeDelta = new Vector2(12 * K, 12 * K);
        Round(dot, new Color32(0x3D, 0xDC, 0x84, 0xFF), 6 * K);
        float rejoinW = 150 * K, rejoinH = 40 * K;
        var rejoinText = Txt(NewRect("Text", banner), medium, 19 * K, Color.white, TextAlignmentOptions.MidlineLeft);
        var rejoinTextRt = (RectTransform)rejoinText.transform;
        rejoinTextRt.anchorMin = Vector2.zero;
        rejoinTextRt.anchorMax = Vector2.one;
        rejoinTextRt.offsetMin = new Vector2(48 * K, 0);
        rejoinTextRt.offsetMax = new Vector2(-(rejoinW + 20 * K), 0);
        rejoinText.textWrappingMode = TextWrappingModes.NoWrap;
        rejoinText.overflowMode = TextOverflowModes.Ellipsis;
        rejoinText.text = "Your game <b>ROOM</b> is still on";
        var rejoin = NewRect("RejoinButton", banner);
        rejoin.anchorMin = rejoin.anchorMax = rejoin.pivot = new Vector2(1, 0.5f);
        rejoin.anchoredPosition = new Vector2(-8 * K, 0);
        rejoin.sizeDelta = new Vector2(rejoinW, rejoinH);
        var rejoinFill = Round(rejoin, Blue, rejoinH / 2);
        rejoinFill.raycastTarget = true;
        var rejoinButton = rejoin.gameObject.AddComponent<Button>();
        rejoinButton.targetGraphic = rejoinFill;
        Tints(rejoinButton);
        var rejoinLabel = Txt(Stretch(NewRect("Label", rejoin)), bold, 18 * K, Color.white, TextAlignmentOptions.Center);
        rejoinLabel.text = "Rejoin";
        UnityEventTools.AddPersistentListener(rejoinButton.onClick, manager.RejoinGame);
        banner.gameObject.SetActive(false);

        manager.rejoinBanner = banner.gameObject;
        manager.rejoinText = rejoinText;
        manager.rejoinButton = rejoinButton;
        manager.rejoinLabel = rejoinLabel;
        manager.startButton = startButton;
        manager.startFill = startFill;
        manager.startLabel = startLabel;
        manager.startArrow = chevron.gameObject;
        manager.startSpinner = spinner.gameObject;
        manager.quitGroup = quitGroup;
        manager.versionLabel = versionLabel;
        EditorUtility.SetDirty(manager);

        Save(scene, "Main menu built");
    }

    // ── Loading ──────────────────────────────────────────────────────────────

    [MenuItem("BioValue/Build Loading Screen")]
    public static void BuildLoading()
    {
        if (!Ready("Build Loading Screen") || !Open(LoadingScenePath, out var scene)) return;

        var canvas = FindCanvas(scene);
        var connection = Object.FindFirstObjectByType<PhotonConnection>(FindObjectsInactive.Include);
        if (!canvas || !connection)
        {
            EditorUtility.DisplayDialog("Build Loading Screen", "Couldn't find the Canvas or the PhotonConnection in the Loading scene.", "OK");
            return;
        }
        // The old black fade isn't used any more (and would cover the buttons).
        var fading = Object.FindFirstObjectByType<FadingScript>(FindObjectsInactive.Include);
        if (fading && fading.gameObject.scene == scene) fading.gameObject.SetActive(false);

        var root = NewRoot(canvas, LoadingRootName);
        var screen = root.gameObject.AddComponent<LoadingScreen>();
        Backdrop(root);
        var logo = Logo(root);
        var logoGroup = logo.gameObject.AddComponent<CanvasGroup>();
        Top(logo, MenuLogoTop, MenuLogoWidth, MenuLogoWidth * LogoH / LogoW);

        var content = Top(NewRect("Content", root), 330 * K, 1000, 520);
        var group = content.gameObject.AddComponent<CanvasGroup>();

        // Three board cells taking turns.
        var cellColors = new[] { Pink, Blue, Orange };
        var cells = new Image[3];
        float cellW = 80 * K, cellH = 92 * K, cellGap = 18 * K;
        for (int i = 0; i < 3; i++)
        {
            var cell = NewRect("Cell", content);
            cell.anchorMin = cell.anchorMax = new Vector2(0.5f, 1f);
            cell.pivot = new Vector2(0.5f, 0.5f);
            cell.anchoredPosition = new Vector2((i - 1) * (cellW + cellGap), -cellH / 2);
            cell.sizeDelta = new Vector2(cellW, cellH);
            cells[i] = cell.gameObject.AddComponent<Image>();
            cells[i].sprite = hex;
            cells[i].color = cellColors[i];
            cells[i].raycastTarget = false;
        }

        float y = cellH + 26 * K;
        var status = Txt(Top(NewRect("Status", content), y, 1000, 58), bold, 32 * K, Color.white, TextAlignmentOptions.Center);
        status.text = "Connecting to the server…";
        y += 58 + 4;
        var sub = Txt(Top(NewRect("Sub", content), y, 1000, 34), regular, 19 * K, new Color(1, 1, 1, .7f), TextAlignmentOptions.Center);
        sub.text = "This takes a few seconds.";
        y += 34 + 34 * K;

        // Steps: Server · Lobby · Ready
        var steps = Top(NewRect("Steps", content), y, 600, 90);
        var views = new LoadingScreen.StepView[3];
        float dotSize = 30 * K, spacing = 183f;
        string[] names = { "Server", "Lobby", "Ready" };
        for (int i = 0; i < 3; i++)
        {
            var v = new LoadingScreen.StepView();
            float x = (i - 1) * spacing;
            if (i > 0)
            {
                var line = NewRect("Line", steps);
                line.anchorMin = line.anchorMax = line.pivot = new Vector2(0.5f, 1f);
                line.anchoredPosition = new Vector2(x - spacing / 2, -(dotSize / 2 - 2.5f));
                line.sizeDelta = new Vector2(70 * K, 4 * K);
                v.line = Round(line, Color.white, 2.5f);
            }
            var holder = NewRect(names[i], steps);
            holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 1f);
            holder.pivot = new Vector2(0.5f, 0.5f);
            holder.anchoredPosition = new Vector2(x, -dotSize / 2);
            holder.sizeDelta = new Vector2(dotSize, dotSize);
            v.pulse = Round(Stretch(NewRect("Pulse", holder)), Pink, dotSize / 2);
            v.dot = Round(Stretch(NewRect("Dot", holder)), Pink, dotSize / 2);
            var inner = Centered(NewRect("Inner", holder), dotSize - 2 * 7 * K);
            Round(inner, Color.white, (dotSize - 2 * 7 * K) / 2);
            v.inner = inner.gameObject;
            v.tick = MainHudBuilder.Tick(NewRectStretch("TickHolder", holder), Color.white, 0.8f).parent.gameObject;
            var todo = Stretch(NewRect("Todo", holder)).gameObject.AddComponent<Image>();
            todo.sprite = CircleRing();
            todo.color = new Color(1, 1, 1, .3f);
            todo.raycastTarget = false;
            v.todo = todo.gameObject;
            var label = NewRect("Label", steps);
            label.anchorMin = label.anchorMax = new Vector2(0.5f, 1f);
            label.pivot = new Vector2(0.5f, 1f);
            label.anchoredPosition = new Vector2(x, -(dotSize + 8 * K));
            label.sizeDelta = new Vector2(150, 28);
            v.label = Txt(label, bold, 16 * K, Color.white, TextAlignmentOptions.Center);
            v.label.text = names[i];
            views[i] = v;
        }

        // Cancel, under the steps.
        var cancel = Top(NewRect("CancelButton", content), y + 90 + 26, 160, 36);
        var cancelHit = cancel.gameObject.AddComponent<Image>();
        cancelHit.color = Color.clear;
        var cancelButton = cancel.gameObject.AddComponent<Button>();
        cancelButton.targetGraphic = cancelHit;
        var cancelLabel = Txt(Stretch(NewRect("Label", cancel)), medium, 16 * K, new Color(1, 1, 1, .6f), TextAlignmentOptions.Center);
        cancelLabel.text = "<u>Cancel</u>";
        UnityEventTools.AddPersistentListener(cancelButton.onClick, screen.BackToMenu);

        // No connection: Try again / Back to menu, where the steps were.
        var errorButtons = Top(NewRect("ErrorButtons", content), y, 480, 56 * K);
        float h = 56 * K, tryW = 200, backW = 240, gap = 14 * K, left = -(tryW + gap + backW) / 2;
        var retry = NewRect("TryAgain", errorButtons);
        retry.anchorMin = retry.anchorMax = new Vector2(0.5f, 0.5f);
        retry.pivot = new Vector2(0f, 0.5f);
        retry.anchoredPosition = new Vector2(left, 0);
        retry.sizeDelta = new Vector2(tryW, h);
        var retryFill = Round(retry, Pink, h / 2);
        retryFill.raycastTarget = true;
        var retryButton = retry.gameObject.AddComponent<Button>();
        retryButton.targetGraphic = retryFill;
        Tints(retryButton);
        Txt(Stretch(NewRect("Label", retry)), bold, 21 * K, Color.white, TextAlignmentOptions.Center).text = "Try again";
        UnityEventTools.AddPersistentListener(retryButton.onClick, screen.Retry);
        var back = NewRect("BackToMenu", errorButtons);
        back.anchorMin = back.anchorMax = new Vector2(0.5f, 0.5f);
        back.pivot = new Vector2(0f, 0.5f);
        back.anchoredPosition = new Vector2(left + tryW + gap, 0);
        back.sizeDelta = new Vector2(backW, h);
        var backButton = OutlinedButton(back, "Back to menu", 19 * K, h);
        UnityEventTools.AddPersistentListener(backButton.onClick, screen.BackToMenu);
        errorButtons.gameObject.SetActive(false);

        Version(root);

        screen.connection = connection;
        screen.logo = logo;
        screen.logoGroup = logoGroup;
        screen.logoFromPosition = new Vector2(0, -MenuLogoTop);
        screen.logoFromSize = new Vector2(MenuLogoWidth, MenuLogoWidth * LogoH / LogoW);
        screen.logoToPosition = new Vector2(0, -LoadingLogoTop);
        screen.logoToSize = new Vector2(LoadingLogoWidth, LoadingLogoWidth * LogoH / LogoW);
        screen.content = group;
        screen.cells = cells;
        screen.cellColors = cellColors;
        screen.status = status;
        screen.sub = sub;
        screen.steps = steps.gameObject;
        screen.stepViews = views;
        screen.errorButtons = errorButtons.gameObject;
        screen.cancelButton = cancel.gameObject;
        EditorUtility.SetDirty(screen);

        Save(scene, "Loading screen built");
    }

    // ── Shared ───────────────────────────────────────────────────────────────

    internal static bool Ready(string title)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(title, "Stop Play mode first: the builder edits the scene.", "OK");
            return false;
        }
        if (!LoadArt())
        {
            EditorUtility.DisplayDialog(title,
                "Missing art or font. Run BioValue/Build Instrument Piece and BioValue/Build Action Card first.", "OK");
            return false;
        }
        return true;
    }

    internal static bool Open(string path, out Scene scene)
    {
        scene = EditorSceneManager.GetActiveScene();
        if (scene.path == path) return true;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
        scene = EditorSceneManager.OpenScene(path);
        return true;
    }

    internal static Canvas FindCanvas(Scene scene)
    {
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.gameObject.scene == scene && c.name == "Canvas") return c;
        return null;
    }

    // A fresh root under the Canvas (replaces the last build); everything else there is switched off.
    internal static RectTransform NewRoot(Canvas canvas, string name)
    {
        var canvasT = (RectTransform)canvas.transform;
        var old = canvasT.Find(name);
        if (old) Object.DestroyImmediate(old.gameObject);
        foreach (Transform child in canvasT) child.gameObject.SetActive(false);
        var root = Stretch(NewRect(name, canvasT));
        root.SetAsFirstSibling();
        return root;
    }

    internal static void Save(Scene scene, string what)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MainMenuBuilder] " + what + " (the old objects are switched off under the Canvas).");
    }

    // Radial purple and the floating hexes: the same in both scenes.
    // `faint`: the Lobby's fewer, paler hexes, so its white cards read well.
    internal static void Backdrop(RectTransform root, bool faint = false)
    {
        var bg = Stretch(NewRect("Background", root)).gameObject.AddComponent<Image>();
        bg.sprite = Gradient();
        bg.raycastTarget = false;

        // x, y, width in mockup units; colour, opacity, rotation, delay.
        var hexes = Stretch(NewRect("Hexes", root));
        if (faint)
        {
            Hex(hexes, 60, 150, 90, Pink, .35f, -12, 0f);
            Hex(hexes, 150, 640, 60, Orange, .35f, 8, 1.2f);
            Hex(hexes, 1310, 110, 80, Blue, .35f, 14, .6f);
            Hex(hexes, 1290, 600, 110, Orange, .3f, -6, 1.8f);
            Hex(hexes, 1180, 700, 44, Pink, .35f, 10, 2.6f);
            return;
        }
        Hex(hexes, 140, 120, 110, Pink, .9f, -12, 0f);
        Hex(hexes, 250, 560, 70, Orange, .9f, 8, 1.2f);
        Hex(hexes, 80, 400, 44, Blue, .8f, 0, 2.1f);
        Hex(hexes, 1180, 150, 90, Blue, .9f, 14, .6f);
        Hex(hexes, 1250, 520, 120, Orange, .85f, -6, 1.8f);
        Hex(hexes, 1110, 640, 50, Pink, .9f, 10, 2.6f);
        Hex(hexes, 1340, 360, 40, Color.white, .25f, 0, 3f);
    }

    // Official horizontal logo (size and place set by the caller).
    internal static RectTransform Logo(RectTransform root)
    {
        var logo = NewRect("Logo", root);
        LogoPart(logo, "Symbol", Single(SymbolPath, SymbolSource), 0, SymbolW / LogoW);
        LogoPart(logo, "Wordmark", Single(WordmarkPath, null), WordmarkX / LogoW, WordmarkW / LogoW);
        return logo;
    }

    internal static TextMeshProUGUI Version(RectTransform root)
    {
        var version = NewRect("Version", root);
        version.anchorMin = version.anchorMax = version.pivot = new Vector2(1, 0);
        version.anchoredPosition = new Vector2(-24 * K, 16 * K);
        version.sizeDelta = new Vector2(200, 28);
        var label = Txt(version, regular, 14 * K, new Color(1, 1, 1, .4f), TextAlignmentOptions.BottomRight);
        label.text = "v" + PlayerSettings.bundleVersion; // kept in step at runtime by VersionLabel
        return label;
    }

    // Transparent pill with a white outline, clickable everywhere.
    internal static Button OutlinedButton(RectTransform rt, string text, float fontSize, float height)
    {
        var hit = rt.gameObject.AddComponent<Image>();
        hit.color = Color.clear;
        var ring = Stretch(NewRect("Outline", rt)).gameObject.AddComponent<Image>();
        ring.sprite = RoundRing();
        ring.type = Image.Type.Sliced;
        ring.pixelsPerUnitMultiplier = 24f / (height / 2);
        ring.color = new Color(1, 1, 1, .35f);
        ring.raycastTarget = false;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = ring;
        Tints(button);
        Txt(Stretch(NewRect("Label", rt)), bold, fontSize, Color.white, TextAlignmentOptions.Center).text = text;
        return button;
    }

    // Centred horizontally, `top` units from the top of the parent.
    internal static RectTransform Top(RectTransform rt, float top, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -top);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    internal static RectTransform Centered(RectTransform rt, float size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(size, size);
        return rt;
    }

    private static RectTransform NewRectStretch(string name, Transform parent) => Stretch(NewRect(name, parent));

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
    internal static void Hex(RectTransform parent, float x, float y, float w, Color color, float alpha, float rotation, float delay)
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

    internal static void Tints(Button button)
    {
        var colors = button.colors;
        colors.normalColor = colors.selectedColor = colors.disabledColor = Color.white;
        colors.highlightedColor = new Color(0.93f, 0.93f, 0.93f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        button.colors = colors;
    }

    // ── Art ──────────────────────────────────────────────────────────────────

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

    private delegate Color PixelAt(float x, float y, int size);

    // Draws `pixel` into ArtFolder/name once, then imports it as one sprite.
    private static Sprite Generated(string name, int w, int h, PixelAt pixel, Vector4 border, bool alpha)
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
        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single
            || importer.spriteBorder != border)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = alpha;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // radial-gradient(120% 90% at 50% 40%, #5434C8 0%, #3F27A0 45%, #241A5E 100%)
    internal static Sprite Gradient()
    {
        Color a = new Color32(0x54, 0x34, 0xC8, 0xFF), b = new Color32(0x3F, 0x27, 0xA0, 0xFF), c = new Color32(0x24, 0x1A, 0x5E, 0xFF);
        const int w = 480, h = 270;
        return Generated("MenuBackground.png", w, h, (x, y, _) =>
        {
            float dx = x / w - 0.5f, dy = (h - y) / h - 0.4f; // texture rows go up
            float t = Mathf.Sqrt(dx / 1.2f * (dx / 1.2f) + dy / 0.9f * (dy / 0.9f));
            return t < 0.45f ? Color.Lerp(a, b, t / 0.45f) : Color.Lerp(b, c, Mathf.Clamp01((t - 0.45f) / 0.55f));
        }, Vector4.zero, false);
    }

    // White rounded-rectangle outline (2 px at radius 24), 9-sliced.
    internal static Sprite RoundRing()
    {
        const int r = 24;
        return Generated("RoundRing.png", 96, 96, (x, y, size) =>
        {
            float cx = Mathf.Clamp(x, r, size - r), cy = Mathf.Clamp(y, r, size - r);
            float fromEdge = r - Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)); // inwards
            return new Color(1, 1, 1, Mathf.Clamp01(fromEdge + 0.5f) * Mathf.Clamp01(2f - fromEdge + 0.5f));
        }, new Vector4(r, r, r, r), true);
    }

    // White circle outline: a step not reached yet (6 px of 64).
    internal static Sprite CircleRing() => Generated("CircleRing.png", 64, 64, (x, y, size) =>
    {
        float fromEdge = size / 2f - Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f));
        return new Color(1, 1, 1, Mathf.Clamp01(fromEdge + 0.5f) * Mathf.Clamp01(6f - fromEdge + 0.5f));
    }, Vector4.zero, true);

    // Faint ring with a bright quarter at the top: turned by MenuSpin.
    private static Sprite Spinner() => Generated("Spinner.png", 64, 64, (x, y, size) =>
    {
        float dx = x - size / 2f, dy = y - size / 2f;
        float fromEdge = size / 2f - Mathf.Sqrt(dx * dx + dy * dy);
        float ring = Mathf.Clamp01(fromEdge + 0.5f) * Mathf.Clamp01(10f - fromEdge + 0.5f);
        float angle = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg; // 0 at the top, clockwise
        float bright = angle >= -45f && angle <= 45f ? 1f : 0.35f;
        return new Color(1, 1, 1, ring * bright);
    }, Vector4.zero, true);
}
