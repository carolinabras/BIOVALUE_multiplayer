using UnityEngine;
using UnityEngine.UI;

// Lays children out left to right at their preferred size and wraps to a new row when the
// next one doesn't fit (like text). Used for the instrument tray's category chips.
public class FlowLayoutGroup : LayoutGroup
{
    public float spacingX = 6f;
    public float spacingY = 6f;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        SetLayoutInputForAxis(padding.horizontal, padding.horizontal, -1, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
        float h = Arrange(false);
        SetLayoutInputForAxis(h, h, -1, 1);
    }

    public override void SetLayoutHorizontal() => Arrange(true);
    public override void SetLayoutVertical() => Arrange(true);

    // Returns the total height the rows need.
    private float Arrange(bool apply)
    {
        float width = rectTransform.rect.width - padding.horizontal;
        float x = 0f, y = 0f, rowHeight = 0f;
        foreach (var child in rectChildren)
        {
            float w = LayoutUtility.GetPreferredWidth(child);
            float h = LayoutUtility.GetPreferredHeight(child);
            if (x > 0f && x + w > width)
            {
                x = 0f;
                y += rowHeight + spacingY;
                rowHeight = 0f;
            }
            if (apply)
            {
                SetChildAlongAxis(child, 0, padding.left + x, w);
                SetChildAlongAxis(child, 1, padding.top + y, h);
            }
            x += w + spacingX;
            rowHeight = Mathf.Max(rowHeight, h);
        }
        return padding.vertical + y + rowHeight;
    }
}
