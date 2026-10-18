using UnityEngine;
using UnityEngine.UI;

// On a board cell with the raised-tile look: while an instrument sits on the cell, the tile's side
// takes the instrument's colour (a shade darker), so the piece reads as a coloured tile.
// Placing: the coloured side slides out from under the tile with a small bounce while the colour fades in.
// Removing: the side tucks back under the tile and comes out grey again.
// Checks the board a few times a second instead of hooking every way a piece can arrive or leave
// (drag, tap, network sync, a reverted vote).
// Added by the editor menu "BioValue/Build Board".
[RequireComponent(typeof(BoardCell))]
public class BoardTileTint : MonoBehaviour
{
    public Image side;
    [Range(0f, 1f)] public float darken = 0.78f;
    public float placeTime = 0.45f;
    public float removeTime = 0.4f;

    private Color emptyColor;
    private Vector2 sideMin, sideMax;      // the side's offsets at full depth
    private float depth;                    // how far below the tile the side sits (px, full = recorded)
    private InstrumentHook current;
    private float nextCheck;

    // Running animation
    private bool animating, placing;
    private float animTime, animDuration;
    private Color fromColor, toColor;

    private void Awake()
    {
        if (!side) return;
        emptyColor = side.color;
        var rt = side.rectTransform;
        sideMin = rt.offsetMin;
        sideMax = rt.offsetMax;
        // Offsets carry the drop (both ends moved down by the same amount); measure it against the tile.
        var top = transform.Find("Top") as RectTransform;
        depth = top ? top.offsetMax.y - rt.offsetMax.y : 0f;
    }

    private void Update()
    {
        if (!side) return;
        if (animating) Animate();

        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 0.25f;

        var hook = PieceOnThisCell();
        if (hook == current) return;
        current = hook;
        StartAnimation(hook != null, hook ? SideColor(hook) : emptyColor);
    }

    private void StartAnimation(bool place, Color target)
    {
        placing = place;
        fromColor = side.color;
        toColor = target;
        animTime = 0f;
        animDuration = place ? placeTime : removeTime;
        animating = true;
        if (!isActiveAndEnabled || animDuration <= 0f) FinishAnimation();
    }

    private void Animate()
    {
        animTime += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(animTime / animDuration);
        if (t >= 1f) { FinishAnimation(); return; }

        float drop;
        if (placing)
        {
            // Slides out from under the tile and overshoots a little before settling.
            drop = EaseOutBack(t);
            side.color = Color.Lerp(fromColor, toColor, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t * 1.6f)));
        }
        else
        {
            // Tucks under the tile (colour changes while hidden), then comes back out grey.
            drop = 1f - Mathf.Sin(t * Mathf.PI) * 0.9f;
            side.color = Color.Lerp(fromColor, toColor, Mathf.SmoothStep(0f, 1f, t * 2f - 0.3f));
        }
        SetDrop(drop);
    }

    private void FinishAnimation()
    {
        animating = false;
        side.color = toColor;
        SetDrop(1f);
    }

    // 0 = hidden right under the tile, 1 = normal depth.
    private void SetDrop(float amount)
    {
        var shift = new Vector2(0f, depth * (1f - amount));
        side.rectTransform.offsetMin = sideMin + shift;
        side.rectTransform.offsetMax = sideMax + shift;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }

    // A piece on the board is a sibling of the cells, snapped to the cell's position.
    private InstrumentHook PieceOnThisCell()
    {
        var parent = transform.parent;
        if (!parent) return null;
        var cellPos = ((RectTransform)transform).anchoredPosition;
        foreach (Transform t in parent)
        {
            if (t == transform) continue;
            var hook = t.GetComponent<InstrumentHook>();
            if (!hook || hook.instrument == null) continue;
            if ((((RectTransform)t).anchoredPosition - cellPos).sqrMagnitude < 25f) return hook;
        }
        return null;
    }

    private Color SideColor(InstrumentHook hook)
    {
        var view = hook.GetComponent<InstrumentPieceView>();
        var style = view ? view.StyleFor(hook.instrument.typeOne) : null;
        if (style == null) return emptyColor;
        var c = style.ring * darken;
        c.a = 1f;
        return c;
    }
}
