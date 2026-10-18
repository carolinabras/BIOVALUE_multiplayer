using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Sharper text everywhere: every UI text (TextMeshPro) in every scene is rendered at a bigger
// font size inside a bigger box, then scaled down so it looks the same size on screen.
//
// Starts by itself when the game runs - nothing to place in scenes. In Play mode, select
// "[TextSupersampler]" under DontDestroyOnLoad to switch it off or change the factor live.
//
// Skipped on purpose:
//  - texts placed by a Layout Group, or sized by a Content Size Fitter (the trick breaks their spacing)
//  - anything under an object with the NoTextSupersampling component
public class TextSupersampler : MonoBehaviour
{
    [SerializeField] private bool supersample = true;
    [Tooltip("How many times bigger the text is rendered before being scaled back down.")]
    [Range(1f, 4f)]
    [SerializeField] private float factor = 3f;

    private static TextSupersampler instance;
    private static readonly HashSet<TMP_Text> pending = new HashSet<TMP_Text>();

    private readonly List<SupersampledText> applied = new List<SupersampledText>();
    private bool lastSupersample;
    private float lastFactor;
    private float nextScanTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        pending.Clear();
        var go = new GameObject("[TextSupersampler]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<TextSupersampler>();

        // Fires whenever any text is (re)built, so texts spawned at runtime are picked up too.
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
    }

    private void Awake()
    {
        lastSupersample = supersample;
        lastFactor = factor;
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        instance = null;
    }

    private static void OnTextChanged(Object obj)
    {
        // Only queue here: changing sizes while the canvas is rebuilding is not allowed.
        if (obj is TextMeshProUGUI text) pending.Add(text);
    }

    private void LateUpdate()
    {
        if (supersample != lastSupersample || !Mathf.Approximately(factor, lastFactor))
        {
            RevertAll();
            lastSupersample = supersample;
            lastFactor = factor;
            QueueAllActiveTexts();
        }

        // Safety net in case a text was missed by the event.
        if (Time.unscaledTime >= nextScanTime)
        {
            nextScanTime = Time.unscaledTime + 1f;
            QueueAllActiveTexts();
        }

        if (pending.Count == 0) return;
        var batch = new List<TMP_Text>(pending);
        pending.Clear();

        if (!supersample || factor <= 1f) return;
        foreach (var text in batch)
            TryApply(text);
    }

    private void QueueAllActiveTexts()
    {
        foreach (var text in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var target = TargetFor(text);
            var existing = target.GetComponent<SupersampledText>();
            if (existing == null || !existing.IsApplied) pending.Add(text);
        }
    }

    private void TryApply(TMP_Text text)
    {
        if (!text || !text.isActiveAndEnabled) return;
        if (text.GetComponentInParent<NoTextSupersampling>(true)) return;

        Transform target = TargetFor(text);
        var existing = target.GetComponent<SupersampledText>();
        if (existing && existing.IsApplied) return;

        // Don't stack the trick inside something that already has it.
        var parent = target.parent;
        if (parent && parent.GetComponentInParent<SupersampledText>() is SupersampledText outer && outer.IsApplied) return;

        if (!IsFreeToScale((RectTransform)target)) return;

        var s = existing ? existing : target.gameObject.AddComponent<SupersampledText>();
        s.Apply(factor);
        applied.Add(s);
    }

    // Texts inside an input field are handled through the whole field (text, placeholder, caret, mask).
    private static Transform TargetFor(TMP_Text text)
    {
        var field = text.GetComponentInParent<TMP_InputField>(true);
        return field ? field.transform : text.transform;
    }

    // True when nothing else controls this box's size, so it can be enlarged and scaled down safely.
    public static bool IsFreeToScale(RectTransform rt)
    {
        if (!rt) return false;
        if (rt.GetComponent<ContentSizeFitter>() || rt.GetComponent<AspectRatioFitter>()) return false;

        var parent = rt.parent;
        if (parent && parent.GetComponent<LayoutGroup>() is LayoutGroup group && group.enabled)
        {
            var le = rt.GetComponent<LayoutElement>();
            if (le == null || !le.ignoreLayout) return false;
        }
        return true;
    }

    private void RevertAll()
    {
        foreach (var s in applied)
            if (s) s.Revert();
        applied.Clear();
    }
}
