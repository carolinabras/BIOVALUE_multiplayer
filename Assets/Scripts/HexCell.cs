using UnityEngine;

public class HexCell : MonoBehaviour
{
    public bool isOccupied = false;
    
    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
    }

    public bool IsFree()
    {
        return !isOccupied;
    }
}
