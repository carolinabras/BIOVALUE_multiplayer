using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class HexMapController : MonoBehaviour
{
    public static HexMapController Instance;

   
    [SerializeField] private GameObject hexCellPrefab;
    [SerializeField] private RectTransform gridParent;
    public RectTransform GridParent => gridParent;

    [SerializeField] private int radius = 3;
    
    [SerializeField] private float hexSize = 47f; 

  
   // fix spacing
    [SerializeField] private float xSpacingFix = 0f;
    [SerializeField] private float ySpacingFix = 1f; 

  

    private readonly Dictionary<Vector2Int, GameObject> cells = new();

    const float SQRT3 = 1.7320508075688772f;

    private void Awake() => Instance = this;

    private void Start() => GenerateHexGrid();

    public GameObject GetCell(int q, int r)
    {
        var key = new Vector2Int(q, r);
        return cells.TryGetValue(key, out var go) ? go : null;
    }
    
    public IEnumerable<GameObject> GetAllCells()
    {
        return cells.Values;
    }

    private void GenerateHexGrid()
    {
        cells.Clear();

       
        float width  = SQRT3 * hexSize; 
        float height = 2f * hexSize;   

    
        float stepX = width + xSpacingFix;          
        float stepY = height * 0.75f + ySpacingFix;  

        for (int q = -radius; q <= radius; q++)
        {
            int r1 = Mathf.Max(-radius, -q - radius);
            int r2 = Mathf.Min(radius, -q + radius);

            for (int r = r1; r <= r2; r++)
            {
                var axial = new Vector2Int(q, r);

                
                float x = stepX * (q + r * 0.5f);
                float y = stepY * r;
                var pos = new Vector2(x, y);

                var cell = Instantiate(hexCellPrefab, gridParent);
                var rect = cell.GetComponent<RectTransform>();

           
                rect.sizeDelta = new Vector2(width, height);
                rect.anchoredPosition = pos;

         
                var drop = cell.GetComponent<HexCellDrop>();
                if (drop != null) drop.axialCoords = axial;

                var txt = cell.GetComponentInChildren<TextMeshProUGUI>();
                if (txt) txt.text = $"{q},{r}";

                cells[axial] = cell;
            }
        }
    }
}