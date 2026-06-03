using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to LogPanel. Appends every game-log entry to a single TMP_Text
/// inside the scroll view — no dynamic child GameObjects, no layout conflicts.
/// All fields are auto-discovered at runtime; nothing needs to be wired in the Inspector.
/// </summary>
public class GameLogUI : MonoBehaviour
{
    [SerializeField] private ScrollRect   scrollRect;
    [SerializeField] private TextMeshProUGUI logText;

    private bool _subscribed = false;

    private void OnEnable()
    {
        Discover();
        // Clear any stale text (e.g. hardcoded editor text) before subscribing.
        if (logText != null) logText.text = string.Empty;
        TrySubscribe();
    }

    private void Start()
    {
        // Fallback: if OnEnable ran before GameLog.Awake (null instance), subscribe now.
        TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (_subscribed) return;

        if (GameLog.Instance == null) return;

        if (logText == null)
        {
            Debug.LogError($"[GameLogUI] No TextMeshProUGUI found under '{gameObject.name}'. " +
                           "Expected: LogPanel/ScrollView/Viewport/LogContent with a TextMeshProUGUI. " +
                           "You can also assign the Log Text field directly in the Inspector.");
            return;
        }

        if (scrollRect == null)
            Debug.LogWarning("[GameLogUI] No ScrollRect found — log will not auto-scroll.");

        logText.text = GameLog.Instance.Entries.Count > 0
            ? string.Join("\n", GameLog.Instance.Entries)
            : string.Empty;
        ScrollToBottom();

        GameLog.Instance.OnEntryAdded.AddListener(OnEntryAdded);
        _subscribed = true;
        Debug.Log($"[GameLogUI] Subscribed — logText='{logText.gameObject.name}', " +
                  $"scrollRect='{(scrollRect ? scrollRect.gameObject.name : "none")}', " +
                  $"entries replayed={GameLog.Instance.Entries.Count}");
    }

    private void OnDisable()
    {
        if (_subscribed)
            GameLog.Instance?.OnEntryAdded.RemoveListener(OnEntryAdded);
        _subscribed = false;
    }

    private void OnEntryAdded(string entry)
    {
        if (logText == null) { Discover(); if (logText == null) return; }
        logText.text += (logText.text.Length > 0 ? "\n" : "") + entry;
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    [ContextMenu("Test Log Entry")]
    private void TestLogEntry()
    {
        Discover();
        if (logText == null) { Debug.LogError("[GameLogUI] logText is null — check hierarchy."); return; }
        logText.text += (logText.text.Length > 0 ? "\n" : "") + $"[TEST] Log works at {System.DateTime.Now:HH:mm:ss}";
        Debug.Log("[GameLogUI] Test entry written.");
    }

    // Right-click this component → "Fix Scroll Clipping" to repair the viewport mask.
    [ContextMenu("Fix Scroll Clipping")]
    private void FixScrollClipping()
    {
        Discover();

        // Find the Viewport under ScrollView.
        var viewportTf = transform.Find("ScrollView/Viewport");
        if (viewportTf == null)
        {
            Debug.LogError("[GameLogUI] Could not find ScrollView/Viewport under this GameObject.");
            return;
        }

        var viewport = viewportTf.gameObject;

        // RectMask2D is lighter than Mask and needs no Image — prefer it.
        // Remove legacy Mask if present to avoid conflicts.
        var oldMask = viewport.GetComponent<UnityEngine.UI.Mask>();
        if (oldMask != null)
        {
            DestroyImmediate(oldMask);
            Debug.Log("[GameLogUI] Removed legacy Mask component.");
        }

        if (viewport.GetComponent<RectMask2D>() == null)
        {
            viewport.AddComponent<RectMask2D>();
            Debug.Log("[GameLogUI] Added RectMask2D to Viewport.");
        }
        else
        {
            Debug.Log("[GameLogUI] RectMask2D already present on Viewport.");
        }

        // Make sure ScrollRect.viewport is wired to this Viewport.
        if (scrollRect != null && scrollRect.viewport == null)
        {
            scrollRect.viewport = viewportTf as RectTransform;
            Debug.Log("[GameLogUI] Assigned ScrollRect.viewport.");
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
        Debug.Log("[GameLogUI] Fix applied — save the scene (Ctrl+S) to persist.");
#endif
    }

    private void Discover()
    {
        if (scrollRect == null)
            scrollRect = GetComponentInChildren<ScrollRect>(includeInactive: true);

        if (logText == null)
        {
            var contentTf = transform.Find("ScrollView/Viewport/LogContent");
            if (contentTf != null)
                logText = contentTf.GetComponent<TextMeshProUGUI>();
        }

        if (logText == null)
            logText = GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
    }
}
