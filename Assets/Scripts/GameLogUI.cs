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

    private void Start()
    {
        Discover();

        if (GameLog.Instance == null)
        {
            Debug.LogWarning("[GameLogUI] GameLog not found in scene.");
            return;
        }

        // Replay history already in the log (covers rejoiners and late Start() calls).
        if (logText != null && GameLog.Instance.Entries.Count > 0)
        {
            logText.text = string.Join("\n", GameLog.Instance.Entries);
            ScrollToBottom();
        }

        GameLog.Instance.OnEntryAdded.AddListener(OnEntryAdded);
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

    private void Discover()
    {
        // ScrollRect: first one found in children (ScrollView).
        if (scrollRect == null)
            scrollRect = GetComponentInChildren<ScrollRect>(includeInactive: true);

        // TMP_Text: on LogContent (created by GameLogSetup).
        if (logText == null)
        {
            // Try path-based search first (most reliable).
            var contentTf = transform.Find("ScrollView/Viewport/LogContent");
            if (contentTf != null)
                logText = contentTf.GetComponent<TextMeshProUGUI>();
        }

        // Fallback: any TMP_Text in children.
        if (logText == null)
            logText = GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
    }
}
