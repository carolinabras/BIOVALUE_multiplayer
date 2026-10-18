using UnityEngine;

// Scales a fixed-size design (e.g. the 300x420 action card) uniformly to fit this object's rect,
// so the same prefab works in the deck grid, the played-cards panel and the summary.
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class FitChildToRect : MonoBehaviour
{
    public RectTransform content;
    public Vector2 referenceSize = new Vector2(300, 420);

    private void OnEnable() => Fit();
    private void OnRectTransformDimensionsChange() => Fit();

    private void Fit()
    {
        if (!content || referenceSize.x <= 0 || referenceSize.y <= 0) return;
        var r = ((RectTransform)transform).rect;
        if (r.width <= 0 || r.height <= 0) return;
        float s = Mathf.Min(r.width / referenceSize.x, r.height / referenceSize.y);
        content.localScale = new Vector3(s, s, 1f);
    }
}
