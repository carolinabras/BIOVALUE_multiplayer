#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CanvasScalerFix
{
    private const float RefWidth  = 1920f;
    private const float RefHeight = 1080f;
    private const float Match     = 0.5f;

    // ── Full Screen Build Settings ────────────────────────────────────────────

    [MenuItem("BioValue/Apply Full Screen Player Settings")]
    public static void ApplyFullScreenSettings()
    {
        PlayerSettings.fullScreenMode       = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultIsFullScreen  = true;
        PlayerSettings.defaultScreenWidth   = 1920;
        PlayerSettings.defaultScreenHeight  = 1080;
        PlayerSettings.resizableWindow      = false;
        PlayerSettings.runInBackground      = true;

        AssetDatabase.SaveAssets();
        Debug.Log("[BioValue] Full screen player settings applied.");
    }

    // ── Canvas Scalers ────────────────────────────────────────────────────────

    [MenuItem("BioValue/Fix Canvas Scalers in All Scenes")]
    public static void FixAll()
    {
        string currentScenePath = EditorSceneManager.GetActiveScene().path;

        string[] scenePaths = new[]
        {
            "Assets/Scenes/Loading.unity",
            "Assets/Scenes/Lobby.unity",
            "Assets/Scenes/Main Menu.unity",
            "Assets/Scenes/MainGame.unity",
            "Assets/Scenes/Role.unity",
        };

        foreach (string path in scenePaths)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int count = FixScalersInActiveScene();
            if (count > 0)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[CanvasScalerFix] {path} — fixed {count} canvas(es).");
            }
            else
            {
                Debug.Log($"[CanvasScalerFix] {path} — nothing to change.");
            }
        }

        if (!string.IsNullOrEmpty(currentScenePath))
            EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);

        Debug.Log("[CanvasScalerFix] Done.");
    }

    [MenuItem("BioValue/Fix Canvas Scalers in Current Scene")]
    public static void FixCurrentScene()
    {
        int count = FixScalersInActiveScene();
        if (count > 0)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log($"[CanvasScalerFix] Fixed {count} canvas(es) in the current scene.");
        }
        else
        {
            Debug.Log("[CanvasScalerFix] Nothing to change in the current scene.");
        }
    }

    private static int FixScalersInActiveScene()
    {
        int count = 0;
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.renderMode == RenderMode.WorldSpace) continue;

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();

            if (scaler.uiScaleMode         == CanvasScaler.ScaleMode.ScaleWithScreenSize        &&
                scaler.referenceResolution  == new Vector2(RefWidth, RefHeight)                  &&
                scaler.screenMatchMode      == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight   &&
                Mathf.Approximately(scaler.matchWidthOrHeight, Match))
                continue;

            Undo.RecordObject(scaler, "Fix CanvasScaler");
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight   = Match;
            EditorUtility.SetDirty(scaler);
            count++;
        }
        return count;
    }
}
#endif
