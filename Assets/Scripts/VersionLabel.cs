using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Every scene's "Version" text shows the game's version (Project Settings → Player → Version),
// so they always match and a new version only needs changing in one place.
public static class VersionLabel
{
    public static string Text => "v" + Application.version;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (_, _) => Apply();
        Apply();
    }

    private static void Apply()
    {
        foreach (var label in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (label.gameObject.name == "Version") label.text = Text;
    }
}
