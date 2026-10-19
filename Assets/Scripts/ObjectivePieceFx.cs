using UnityEngine;
using UnityEngine.UI;

// The objective piece's own glow (mockup "Objective piece", flag tile): a warm gold aura breathing
// behind it, a shine sweeping across its face every few seconds, the flag waving, and a bouncy landing
// when it appears. Nothing pink and pulsing, so it never looks like a free cell or a piece being voted on.
// Built by "BioValue/Build Objective Piece".
public class ObjectivePieceFx : MonoBehaviour
{
    public RectTransform piece;   // lands
    public Image aura;            // breathes
    public RectTransform sheen;   // sweeps across the face
    public RectTransform flag;    // waves

    public float auraPeriod = 2.8f;
    public float sheenPeriod = 3.2f;
    public float sheenFrom = -190f, sheenTo = 190f;
    public float landTime = 0.7f;
    public float tileWidth = 143f; // the piece's tile width in its own units (matched to the board's tile)

    private float _auraAlpha = 1f, _born;
    private bool _waiting;
    private RectTransform _cell;   // the board's centre cell: the piece sits on its tile, which is taken
    private Vector2 _pieceHome;

    private void Awake()
    {
        if (aura) _auraAlpha = aura.color.a;
        if (piece) _pieceHome = piece.anchoredPosition;
    }

    private void OnEnable() => Land();

    // Sits exactly on the board's centre tile (whatever container it was spawned in), at the tile's size.
    private void LateUpdate()
    {
        if (!_cell) _cell = FindCentreCell();
        if (!_cell || !piece) return;
        var root = (RectTransform)transform;
        DrawUnderPieces(root);
        var tile = _cell.Find("Top") as RectTransform ?? _cell;
        Vector3 centre = tile.TransformPoint(tile.rect.center);
        float tileWorld = tile.TransformVector(new Vector3(tile.rect.width, 0f, 0f)).magnitude;
        var parent = root.parent;
        float parentScale = parent ? parent.lossyScale.x : 1f;
        if (tileWorld > 0f && parentScale > 0f) root.localScale = Vector3.one * (tileWorld / (tileWidth * parentScale));
        // piece's resting place (not the landing offset) on the tile's centre
        root.position = centre - root.TransformVector(_pieceHome);
    }

    // Into the board, right after its cells: instruments placed later are added after it, so they
    // draw over the flag where it reaches into a neighbouring cell. Cell indices don't change.
    private void DrawUnderPieces(RectTransform root)
    {
        var board = _cell.parent;
        int afterCells = 0;
        for (int i = 0; i < board.childCount; i++)
            if (board.GetChild(i).GetComponent<BoardCell>()) afterCells = i + 1;
        if (root.parent != board) root.SetParent(board, true);
        int at = afterCells - (root.GetSiblingIndex() < afterCells ? 1 : 0);
        if (root.GetSiblingIndex() != at) root.SetSiblingIndex(at);
    }

    // The cell nearest the middle of all the board's cells; marked taken so no instrument goes there.
    private static RectTransform FindCentreCell()
    {
        var cells = FindObjectsByType<BoardCell>(FindObjectsSortMode.None);
        if (cells.Length == 0) return null;
        Vector3 mid = Vector3.zero;
        foreach (var c in cells) mid += c.transform.position;
        mid /= cells.Length;
        BoardCell best = null;
        float bestD = float.MaxValue;
        foreach (var c in cells)
        {
            float d = (c.transform.position - mid).sqrMagnitude;
            if (d < bestD) { bestD = d; best = c; }
        }
        best.SetOccupied(true);
        return (RectTransform)best.transform;
    }

    // Plays the landing again (also used by the debug "Spawn instruments" button). It waits for the
    // phase intro to finish, so it isn't hidden behind it.
    public void Land()
    {
        _born = Time.unscaledTime;
        _waiting = true;
    }

    public static void LandAll()
    {
        foreach (var fx in FindObjectsByType<ObjectivePieceFx>(FindObjectsSortMode.None)) fx.Land();
    }

    private void Update()
    {
        float now = Time.unscaledTime;

        if (_waiting)
        {
            if (PhaseIntro.Instance && PhaseIntro.Instance.IsPlaying)
            {
                if (piece) piece.localScale = Vector3.zero; // hidden until the intro is gone
                return;
            }
            _waiting = false;
            _born = now;
        }

        if (piece)
        {
            // Drop from above, overshoot a little, settle.
            float t = Mathf.Clamp01((now - _born) / landTime);
            float drop, scale;
            if (t < 0.6f)
            {
                float k = t / 0.6f;
                drop = Mathf.Lerp(70f, -5f, k * (2f - k));
                scale = Mathf.Lerp(1.15f, 0.98f, k);
            }
            else
            {
                float k = Mathf.SmoothStep(0f, 1f, (t - 0.6f) / 0.4f);
                drop = Mathf.Lerp(-5f, 0f, k);
                scale = Mathf.Lerp(0.98f, 1f, k);
            }
            piece.anchoredPosition = _pieceHome + new Vector2(0f, drop);
            piece.localScale = Vector3.one * scale;
        }

        if (aura)
        {
            float b = 0.5f + 0.5f * Mathf.Sin(now * 2f * Mathf.PI / auraPeriod);
            var c = aura.color;
            c.a = _auraAlpha * Mathf.Lerp(0.55f, 1f, b);
            aura.color = c;
            aura.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.95f, 1.05f, b);
        }

        if (sheen)
        {
            // Waits for the first half of the cycle, then crosses the face.
            float p = Mathf.Repeat(now / sheenPeriod, 1f);
            float k = p < 0.55f ? 0f : Mathf.SmoothStep(0f, 1f, (p - 0.55f) / 0.45f);
            var pos = sheen.anchoredPosition;
            pos.x = Mathf.Lerp(sheenFrom, sheenTo, k);
            sheen.anchoredPosition = pos;
        }

        if (flag)
            flag.localRotation = Quaternion.Euler(0f, 0f, 5f * Mathf.Sin(now * 2f * Mathf.PI / 2.4f));
    }
}
