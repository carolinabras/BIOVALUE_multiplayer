using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The "?" that opens "How to play" (HelpScreen), added to every scene but the main menu (it has its own
// "How to play" button) and the loading screen. Placed clear of each scene's own buttons.
// Its look is Resources/HelpButton.prefab, built by "BioValue/Build Help".
public static class HelpButton
{
    private const string HolderName = "HelpButtonCanvas";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (scene, _) => Add(scene);
        Add(SceneManager.GetActiveScene());
    }

    // Where the button sits in each scene, in canvas units (1920 × 1080).
    private static bool Placement(string scene, out Vector2 corner, out Vector2 offset)
    {
        switch (scene)
        {
            case "Main Menu":
            case "Loading":
                corner = offset = Vector2.zero;
                return false;
            case "Lobby": // left of "Leave room"
                corner = new Vector2(1, 1); offset = new Vector2(-249, -37); return true;
            case "Role":  // left of the room name
                corner = new Vector2(1, 1); offset = new Vector2(-320, -37); return true;
                        default:
                corner = new Vector2(1, 1); offset = new Vector2(-24, -24); return true;
        }
    }

    private static void Add(Scene scene)
    {
        if (!Placement(scene.name, out var corner, out var offset)) return;
        foreach (var go in scene.GetRootGameObjects()) if (go.name == HolderName) return;
        var prefab = Resources.Load<GameObject>("HelpButton");
        if (!prefab) return; // not built yet

        var holder = new GameObject(HolderName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(holder, scene);
        var canvas = holder.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900; // over the scene, under pop-ups made later and the help itself
        var scaler = holder.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var button = Object.Instantiate(prefab, holder.transform, false);
        var rt = (RectTransform)button.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = corner;
        rt.anchoredPosition = offset;
        button.GetComponent<Button>().onClick.AddListener(() => HelpScreen.Open());
    }
}
