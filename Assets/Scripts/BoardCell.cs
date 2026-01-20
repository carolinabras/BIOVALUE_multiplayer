using UnityEngine;

public class BoardCell : MonoBehaviour
{
   public int id; 
   
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
