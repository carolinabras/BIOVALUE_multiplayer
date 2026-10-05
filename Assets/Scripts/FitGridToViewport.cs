using UnityEngine;
using UnityEngine.UI;

// Uniformly scales a fixed-column GridLayoutGroup down so it always fits the width of its viewport
// (e.g. the Action Deck on smaller / non-16:9 screens). Never scales above 1.
[ExecuteAlways]
[RequireComponent(typeof(GridLayoutGroup))]
public class FitGridToViewport : MonoBehaviour
{
    [SerializeField] private RectTransform viewport;

    public float FitScale { get; private set; } = 1f;

    private RectTransform rect;
    private GridLayoutGroup grid;
    private float lastViewportWidth = -1f;

    private void Awake()
    {
        rect = (RectTransform)transform;
        grid = GetComponent<GridLayoutGroup>();
        if (!viewport) viewport = rect.parent as RectTransform;
    }

    // Recomputes the size and FitScale without touching localScale (so open/close tweens can use FitScale).
    public void Recalculate()
    {
        if (!rect) Awake();
        if (!viewport) return;

        lastViewportWidth = viewport.rect.width;

        int columns = grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount ? grid.constraintCount : 4;
        float requiredWidth = grid.padding.left + grid.padding.right
                              + columns * grid.cellSize.x
                              + (columns - 1) * grid.spacing.x;

        // Content is exactly as wide as the grid needs, anchored top-center.
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(requiredWidth, rect.sizeDelta.y);
        rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);

        FitScale = Mathf.Min(1f, lastViewportWidth / requiredWidth);
    }

    private void LateUpdate()
    {
        if (!viewport || Mathf.Approximately(viewport.rect.width, lastViewportWidth)) return;

        // Screen/window size changed while visible: refit immediately.
        Recalculate();
        rect.localScale = new Vector3(FitScale, FitScale, 1f);
    }
}
