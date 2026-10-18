using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Added automatically by TextSupersampler to each text or input field it sharpens.
// Keeps the enlarged box matching the original area when the parent resizes, and can undo itself.
[DisallowMultipleComponent]
public class SupersampledText : MonoBehaviour
{
    public bool IsApplied => factor > 1f;

    private float factor = 1f;
    private RectTransform rt;
    private Vector2 baseSizeDelta;
    private bool busy;

    private readonly List<TMP_Text> texts = new List<TMP_Text>();
    private readonly List<RectTransform> children = new List<RectTransform>();
    private readonly List<Image> images = new List<Image>();
    private readonly List<Shadow> shadows = new List<Shadow>();
    private TMP_InputField field;
    private int baseCaretWidth;

    public void Apply(float k)
    {
        if (IsApplied || k <= 1f) return;
        rt = (RectTransform)transform;
        texts.Clear(); children.Clear(); images.Clear(); shadows.Clear();

        field = GetComponent<TMP_InputField>();
        if (field)
        {
            baseCaretWidth = field.caretWidth;
            field.caretWidth = Mathf.Max(1, Mathf.RoundToInt(baseCaretWidth * k));

            // Children are measured in this (now scaled-down) space, so their pixel offsets grow too.
            foreach (var child in GetComponentsInChildren<RectTransform>(true))
            {
                if (child == rt) continue;
                children.Add(child);
                child.anchoredPosition *= k;
                child.sizeDelta *= k;
            }
            texts.AddRange(GetComponentsInChildren<TMP_Text>(true));

            // Keep rounded/sliced backgrounds and outlines the same thickness on screen.
            foreach (var img in GetComponentsInChildren<Image>(true))
            {
                if (img.type != Image.Type.Sliced && img.type != Image.Type.Tiled) continue;
                images.Add(img);
                img.pixelsPerUnitMultiplier /= k;
            }
            foreach (var sh in GetComponentsInChildren<Shadow>(true))
            {
                shadows.Add(sh);
                sh.effectDistance *= k;
            }
        }
        else
        {
            var text = GetComponent<TMP_Text>();
            if (text) texts.Add(text);
        }

        foreach (var text in texts)
            ScaleFont(text, k);

        factor = k;
        baseSizeDelta = rt.sizeDelta;
        var s = rt.localScale;
        rt.localScale = new Vector3(s.x / k, s.y / k, s.z);
        UpdateSize();
    }

    public void Revert()
    {
        if (!IsApplied) return;
        float k = factor;
        factor = 1f;

        foreach (var text in texts) if (text) ScaleFont(text, 1f / k);
        foreach (var child in children)
        {
            if (!child) continue;
            child.anchoredPosition /= k;
            child.sizeDelta /= k;
        }
        foreach (var img in images) if (img) img.pixelsPerUnitMultiplier *= k;
        foreach (var sh in shadows) if (sh) sh.effectDistance /= k;
        if (field) field.caretWidth = baseCaretWidth;

        busy = true;
        var s = rt.localScale;
        rt.localScale = new Vector3(s.x * k, s.y * k, s.z);
        rt.sizeDelta = baseSizeDelta;
        busy = false;
    }

    private static void ScaleFont(TMP_Text text, float k)
    {
        text.fontSize *= k;
        text.fontSizeMin *= k;
        text.fontSizeMax *= k;
        text.margin *= k;
    }

    // Grows the box by the factor around its pivot, so after the 1/factor scale it covers
    // exactly the same area as before. Works for both fixed-size and stretched boxes.
    private void UpdateSize()
    {
        if (!IsApplied || !rt) return;
        busy = true;
        var parent = rt.parent as RectTransform;
        Vector2 anchorSpan = parent ? Vector2.Scale(parent.rect.size, rt.anchorMax - rt.anchorMin) : Vector2.zero;
        Vector2 originalSize = anchorSpan + baseSizeDelta;
        rt.sizeDelta = baseSizeDelta + (factor - 1f) * originalSize;
        busy = false;
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!busy) UpdateSize();
    }

    // Unity doesn't report size changes to objects that are inactive at the time (e.g. a piece's face
    // that is turned away while the piece moves from the tray to a smaller board cell). Without this,
    // the enlarged box keeps the old size and the text overflows its area when it shows again.
    private Vector2 lastParentSize;

    private void OnEnable() => CheckParentSize();
    private void LateUpdate() => CheckParentSize();

    private void CheckParentSize()
    {
        if (!IsApplied || !rt || !(rt.parent is RectTransform parent)) return;
        var size = parent.rect.size;
        if (size == lastParentSize) return;
        lastParentSize = size;
        UpdateSize();
    }

    private void OnTransformParentChanged()
    {
        if (!IsApplied) return;
        if (TextSupersampler.IsFreeToScale(rt)) UpdateSize();
        else Revert(); // moved into a layout: give the layout back a normal box
    }
}
