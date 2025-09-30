using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class HexMapController : MonoBehaviour
{
    public static HexMapController Instance;

    [SerializeField] private GameObject hexCellPrefab;
    [SerializeField] private RectTransform gridParent;
    [SerializeField] private int radius = 3;
    [SerializeField] private float tileSize = 80f;

    private readonly Dictionary<Vector2Int, GameObject> cells = new();

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

        for (int q = -radius; q <= radius; q++)
        {
            int r1 = Mathf.Max(-radius, -q - radius);
            int r2 = Mathf.Min(radius, -q + radius);
            for (int r = r1; r <= r2; r++)
            {
                var axial = new Vector2Int(q, r);
                var pos = AxialToAnchored(axial);

                var cell = Instantiate(hexCellPrefab, gridParent);
                cell.GetComponent<RectTransform>().anchoredPosition = pos;

                // setar coordenadas no drop handler
                var drop = cell.GetComponent<HexCellDrop>();
                if (drop != null) drop.axialCoords = axial;

                // opcional: label de debug
                var txt = cell.GetComponentInChildren<TextMeshProUGUI>();
                if (txt) txt.text = $"{q},{r}";

                cells[axial] = cell;
            }
        }
    }

    private Vector2 AxialToAnchored(Vector2Int a)
    {
        // hex com ponta para cima (pointy-top)
        float w = tileSize;               // largura do rect do tile
        float h = tileSize * 0.8660254f;  // √3/2
        float x = w * (a.x + a.y / 2f);
        float y = h * a.y;
        return new Vector2(x, y);
    }
}
