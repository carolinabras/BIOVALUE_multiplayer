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

    // ── Board Anchor ──────────────────────────────────────────────────────────

    // The Board currently uses a full-stretch anchor (0,0)-(1,1) with a negative
    // sizeDelta subtracted from its parent's rect. That makes its size depend on
    // the parent canvas's rect, which itself varies with screen aspect ratio under
    // CanvasScaler (constant only at the reference 16:9 ratio) — so the Board
    // drifts relative to every other element (cells, buttons, spawners), which all
    // use a fixed-size point anchor instead. This converts the Board to that same
    // fixed-size point-anchor convention, preserving its exact current visual
    // position/size (read live from the RectTransform) so nothing jumps.
    [MenuItem("BioValue/Fix Board Anchor (Current Scene)")]
    public static void FixBoardAnchor()
    {
        GameObject board = GameObject.Find("Board");
        if (board == null)
        {
            Debug.LogError("[CanvasScalerFix] No GameObject named \"Board\" found in the active scene.");
            return;
        }

        var rt = board.GetComponent<RectTransform>();
        if (rt == null)
        {
            Debug.LogError("[CanvasScalerFix] \"Board\" has no RectTransform.");
            return;
        }

        if (rt.pivot != new Vector2(0.5f, 0.5f))
        {
            Debug.LogError($"[CanvasScalerFix] \"Board\" pivot is {rt.pivot}, expected (0.5, 0.5) — " +
                            "fix aborted so the position math below doesn't silently produce a wrong result.");
            return;
        }

        Undo.RecordObject(rt, "Fix Board Anchor");

        // Capture the board's current actual size/position before touching anchors.
        float width  = rt.rect.width;
        float height = rt.rect.height;
        Vector3 worldCenter = rt.position; // valid because pivot is (0.5, 0.5)

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.position  = worldCenter; // restores the exact same visual placement

        EditorUtility.SetDirty(rt);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"[CanvasScalerFix] Board re-anchored to a fixed point anchor: " +
                  $"size=({width:F1}, {height:F1}), anchoredPosition={rt.anchoredPosition}. " +
                  "Verify it still looks right, then save the scene.");
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
