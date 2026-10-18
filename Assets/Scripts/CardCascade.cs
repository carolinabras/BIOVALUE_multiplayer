using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Background decoration of the action deck: small see-through cards in a diagonal (staggered) pattern
// that slide down and to the right forever. The pattern repeats every two rows, so the loop is seamless.
// Fills its own RectTransform; built by "BioValue/Build Action Drawer".
public class CardCascade : MonoBehaviour
{
    public Sprite cardSprite;
    [Tooltip("Corner roundness of the sliced sprite (higher = smaller corners).")]
    public float pixelsPerUnitMultiplier = 4f;
    public Vector2 cardSize = new Vector2(34, 46);
    [Tooltip("Distance between cards in a row (x) and between rows (y).")]
    public Vector2 spacing = new Vector2(120, 110);
    public float tilt = -16f;
    public Color color = new Color(1f, 1f, 1f, 0.28f);
    [Tooltip("Pixels per second downwards.")]
    public float speed = 18f;

    private readonly List<RectTransform> cards = new List<RectTransform>();
    private int cols, rows;
    private Vector2 builtFor;
    private float offset;

    private void OnEnable() => Build();

    private void OnRectTransformDimensionsChange()
    {
        if (isActiveAndEnabled) Build();
    }

    private void Build()
    {
        var size = ((RectTransform)transform).rect.size;
        if (size.x <= 0f || size.y <= 0f || (size == builtFor && cards.Count > 0)) return;
        builtFor = size;

        // Extra rows/columns so cards enter from outside the edges.
        cols = Mathf.CeilToInt(size.x / spacing.x) + 3;
        rows = Mathf.CeilToInt(size.y / spacing.y) + 4;
        int needed = cols * rows;

        while (cards.Count < needed)
        {
            var go = new GameObject("MiniCard", typeof(RectTransform));
            go.layer = gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = cardSize;
            rt.localRotation = Quaternion.Euler(0, 0, tilt);
            var img = go.AddComponent<Image>();
            img.sprite = cardSprite;
            img.type = cardSprite ? Image.Type.Sliced : Image.Type.Simple;
            img.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            img.color = color;
            img.raycastTarget = false;
            cards.Add(rt);
        }
        for (int i = 0; i < cards.Count; i++)
            cards[i].gameObject.SetActive(i < needed);

        Place();
    }

    private void Update()
    {
        if (cards.Count == 0 || speed == 0f) return;
        offset = Mathf.Repeat(offset + speed * Time.unscaledDeltaTime, spacing.y * 2f);
        Place();
    }

    private void Place()
    {
        // Moving two rows down while drifting one column right lands on the same pattern.
        float driftX = offset * spacing.x / (spacing.y * 2f);
        int i = 0;
        for (int r = 0; r < rows; r++)
        {
            float stagger = (r % 2) * spacing.x * 0.5f;
            // Rows 0 and 1 start above the top edge; everything moves down by "offset".
            float y = -((r - 2) * spacing.y + spacing.y * 0.5f) - offset;
            for (int c = 0; c < cols; c++, i++)
            {
                float x = (c - 2) * spacing.x + stagger + driftX;
                cards[i].anchoredPosition = new Vector2(x, y);
            }
        }
    }
}
