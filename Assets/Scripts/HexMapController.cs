using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class HexMapController : MonoBehaviour
{
    public static HexMapController Instance;

    [Header("Setup")]
    [SerializeField] private GameObject hexCellPrefab;
    [SerializeField] private RectTransform gridParent;
    [SerializeField] private int radius = 3;

    [Header("Sizing (pointy-top)")]
    [Tooltip("Raio do hex (centro -> vértice). Recomendo metade da altura visual pretendida.")]
    [SerializeField] private float hexSize = 47f; // se width≈81.4 e height≈94, então hexSize=47

    [Header("Anti-overlap (px)")]
    [Tooltip("Correção horizontal/vertical para evitar sobreposição visual (borda do sprite, antialias, etc.).")]
    [SerializeField] private float xSpacingFix = 0f;
    [SerializeField] private float ySpacingFix = 1f; // 1 px resolve o 'empilhado' do Y

    private readonly Dictionary<Vector2Int, GameObject> cells = new();

    const float SQRT3 = 1.7320508075688772f;

    private void Awake() => Instance = this;

    private void Start() => GenerateHexGrid();

    public GameObject GetCell(int q, int r)
    {
        var key = new Vector2Int(q, r);
        return cells.TryGetValue(key, out var go) ? go : null;
    }

    private void GenerateHexGrid()
    {
        cells.Clear();

        // Dimensões teóricas (Red Blob, pointy-top):
        float width  = SQRT3 * hexSize; // flat-to-flat
        float height = 2f * hexSize;    // point-to-point

        // Passos entre centros:
        float stepX = width + xSpacingFix;           // largura por coluna “skewed”
        float stepY = height * 0.75f + ySpacingFix;  // 0.75 * altura entre linhas + micro-espaço

        for (int q = -radius; q <= radius; q++)
        {
            int r1 = Mathf.Max(-radius, -q - radius);
            int r2 = Mathf.Min(radius, -q + radius);

            for (int r = r1; r <= r2; r++)
            {
                var axial = new Vector2Int(q, r);

                // Axial -> pixel (pointy-top), com os passos ajustados
                float x = stepX * (q + r * 0.5f);
                float y = stepY * r;
                var pos = new Vector2(x, y);

                var cell = Instantiate(hexCellPrefab, gridParent);
                var rect = cell.GetComponent<RectTransform>();

                // Garante que o Rect bate com a teoria
                rect.sizeDelta = new Vector2(width, height);
                rect.anchoredPosition = pos;

                // Meta opcional
                var drop = cell.GetComponent<HexCellDrop>();
                if (drop != null) drop.axialCoords = axial;

                var txt = cell.GetComponentInChildren<TextMeshProUGUI>();
                if (txt) txt.text = $"{q},{r}";

                cells[axial] = cell;
            }
        }
    }
}