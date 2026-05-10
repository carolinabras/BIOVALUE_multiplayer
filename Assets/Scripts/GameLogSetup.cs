#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-time scene wiring helper. Add this component to any GameObject in the scene.
/// It runs immediately in Edit Mode, builds the log UI, then removes itself.
/// After it runs: press Ctrl+S to save the scene permanently.
///
/// Resulting hierarchy under LogPanel:
///   LogPanel               ← GameLogUI added here
///    └── ScrollView        ← ScrollRect (content + viewport wired)
///         └── Viewport     ← Image + Mask (clips text)
///              └── LogContent  ← TMP_Text + ContentSizeFitter (grows as text appends)
///
/// The same GameObject also gets a GameLog component (the network event manager).
/// </summary>
[ExecuteInEditMode]
public class GameLogSetup : MonoBehaviour
{
    private void Awake()
    {
        // ── Find LogPanel ─────────────────────────────────────────────────────
        var logPanel = GameObject.Find("LogPanel");
        if (logPanel == null)
        {
            Debug.LogError("[GameLogSetup] No GameObject named 'LogPanel' found in the scene.");
            SafeDestroy(this);
            return;
        }

        // ── ScrollView ────────────────────────────────────────────────────────
        var scrollViewGO = FindOrCreate("ScrollView", logPanel.transform);
        FillParent(scrollViewGO.GetComponent<RectTransform>());

        var scrollRect = scrollViewGO.GetComponent<ScrollRect>()
                      ?? scrollViewGO.AddComponent<ScrollRect>();
        scrollRect.horizontal   = false;
        scrollRect.vertical     = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // ── Viewport (Mask so text is clipped to the panel) ───────────────────
        var viewportGO = FindOrCreate("Viewport", scrollViewGO.transform);
        FillParent(viewportGO.GetComponent<RectTransform>());

        if (viewportGO.GetComponent<Image>() == null)
            viewportGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // transparent

        if (viewportGO.GetComponent<Mask>() == null)
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;

        // ── LogContent: single TMP_Text that grows via ContentSizeFitter ──────
        var contentGO = FindOrCreate("LogContent", viewportGO.transform);
        var contentRT = contentGO.GetComponent<RectTransform>();

        // Anchor to top-left, full width, zero initial height.
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;
        contentRT.sizeDelta = Vector2.zero;

        // Single TMP_Text — entries are appended as plain text with newlines.
        var tmp = contentGO.GetComponent<TextMeshProUGUI>()
               ?? contentGO.AddComponent<TextMeshProUGUI>();
        tmp.fontSize         = 13f;
        tmp.color            = new Color(0.15f, 0.15f, 0.15f, 1f);
        tmp.raycastTarget    = false;
        tmp.enableWordWrapping = true;
        tmp.alignment        = TextAlignmentOptions.TopLeft;
        tmp.text             = string.Empty;

        // ContentSizeFitter makes LogContent grow vertically as text is added.
        var csf = contentGO.GetComponent<ContentSizeFitter>()
               ?? contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Wire ScrollRect ───────────────────────────────────────────────────
        scrollRect.viewport = viewportGO.GetComponent<RectTransform>();
        scrollRect.content  = contentRT;

        // ── GameLogUI on the panel ────────────────────────────────────────────
        if (logPanel.GetComponent<GameLogUI>() == null)
            logPanel.AddComponent<GameLogUI>();
        // GameLogUI.Start() discovers scrollRect and logText at runtime via path search.

        // ── GameLog on this same GameObject ──────────────────────────────────
        if (GetComponent<GameLog>() == null)
            gameObject.AddComponent<GameLog>();
        gameObject.name = "GameLog";

        // ── Save prompt ───────────────────────────────────────────────────────
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[GameLogSetup] Done — press Ctrl+S to save the scene.");
        }
#endif
        SafeDestroy(this);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObject FindOrCreate(string name, Transform parent)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        var go = new GameObject(name);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, worldPositionStays: false);
        return go;
    }

    private static void FillParent(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SafeDestroy(Object obj)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) DestroyImmediate(obj);
        else
#endif
            Destroy(obj);
    }
}
