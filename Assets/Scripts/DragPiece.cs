using System;
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Photon.Pun;


public class DragPiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    
    private Vector2 originalPosition;
    private Transform originalParent;
    public RectTransform rectTransform;
    public Canvas canvas;
    
    private PhotonView photonView;
    
    private HexCell currentCell; // the cell where the piece is currently placed
  
    [SerializeField] RectTransform gridParent;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        photonView = GetComponent<PhotonView>();
        
    }
   

    // Transform parentAfterDrag;
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!photonView.IsMine) 
        {
            photonView.RequestOwnership();
            return;
        }
        
        //remember the original parent to return to if not dropped on a valid target
        originalPosition = rectTransform.anchoredPosition;
        originalParent = rectTransform.parent;
        rectTransform.SetParent(gridParent, worldPositionStays:false); // move to be child of grid to align positions
        rectTransform.SetAsLastSibling();
        
        if (currentCell != null)
        {
            currentCell.SetOccupied(false);
            currentCell = null;
        }
        
    }

    public void OnDrag(PointerEventData eventData)
    {
       rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor; 

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        SnapToClosestCell();
        Debug.Log("Drag Ended on " + rectTransform.anchoredPosition);

    }

   /* private void SnapToClosestCell()
    {
       // var hmc = HexMapController.Instance;
       // if (hmc == null)
       // {
           // Debug.LogError("HexMapController não encontrado na cena.");
           // rectTransform.anchoredPosition = originalPosition;
            //return;
        //}
       // var cells = HexMapController.Instance.GetAllCells();
        
        //var gridParent = hmc.GridParent;
        //if (gridParent == null)
       // {
           // Debug.Log("GridParent não está atribuído no HexMapController (arrasta o MapGrid no Inspector).");
           // rectTransform.anchoredPosition = originalPosition;
           // return;
        //}
        
         if(!gridParent) return;
        
        
        RectTransform closestCell = null;
        
        float closestDistance = float.MaxValue;
        
        GameObject targetCellObject = null;

        foreach (var cellClone in cells)
        {
            Debug.Log("Checking cell at " + cellClone.GetComponent<RectTransform>().anchoredPosition + " status: " + cellClone.GetComponent<HexCell>().IsFree());
            RectTransform cellRect = cellClone.GetComponent<RectTransform>();
            var cell = cellClone.GetComponent<HexCell>();

            if (cell != null && cell.IsFree())
            {
                float distance = Vector2.Distance(rectTransform.anchoredPosition, cellRect.anchoredPosition);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestCell = cellRect;
                    targetCellObject = cellClone;
                }
            }
        }

        if (closestCell != null)
        {
            rectTransform.anchoredPosition = closestCell.anchoredPosition;
            targetCellObject.GetComponent<HexCell>().SetOccupied(true);
        }
        else
        {
            // If no valid cell found, return to original position
            Debug.Log("No valid cell found, returning to original position.");
            rectTransform.anchoredPosition = originalPosition;
        }
    }*/

   /*private void SnapToClosestCell()
   {
       if (!gridParent) return;
       
       RectTransform closestCell = null;
       float closestDistance = float.MaxValue;
       
       HexCell closestCellComponent = null;
       
       Vector2 currentPos = rectTransform.anchoredPosition; // current position of the piece

       for (int i = 0; i < gridParent.childCount; i++)
       {
           var cell = gridParent.GetChild(i).GetComponent<HexCell>();
           var cellChildTranform = gridParent.GetChild(i) as RectTransform;
           if (!cellChildTranform || cellChildTranform == rectTransform) continue; // ignore self 
           
           if (cell != null && !cell.IsFree()) continue; // ignore occupied cells
           float distance = Vector2.Distance(cellChildTranform.anchoredPosition, currentPos);

           if (distance < closestDistance)
           {
               closestDistance = distance;
               closestCell = cellChildTranform;
               closestCellComponent = cell;
               if (closestCellComponent != null) closestCellComponent.SetOccupied(true);

               
               currentCell = closestCellComponent;
               
           }
       }
       
       if (closestCell != null)
       {
           rectTransform.anchoredPosition = closestCell.anchoredPosition;
           
       }
       else
       {
           // If no valid cell found, return to original position
           Debug.Log("No valid cell found, returning to original position.");
           rectTransform.anchoredPosition = originalPosition;
           rectTransform.SetParent(originalParent, worldPositionStays:false); // move to original parent
       }
   }
    */
  
   private void SnapToClosestCell()
   {
       if (!gridParent) return;

       RectTransform closestCell = null;
       HexCell closestCellComponent = null;
       float closestDistance = float.MaxValue;

       Vector2 currentPos = rectTransform.anchoredPosition;

       for (int i = 0; i < gridParent.childCount; i++)
       {
           var child = gridParent.GetChild(i);
           var cellChildTranform = child as RectTransform;
           if (!cellChildTranform || cellChildTranform == rectTransform) continue; // ignore self

           var cell = child.GetComponent<HexCell>();
           if (cell == null) continue;          
           if (!cell.IsFree()) continue;        // ignore occupied cells

           float distance = Vector2.Distance(cellChildTranform.anchoredPosition, currentPos);
           if (distance < closestDistance)
           {
               closestDistance = distance;
               closestCell = cellChildTranform;
               closestCellComponent = cell;     
           }
       }

       if (closestCell != null && closestCellComponent != null && closestCellComponent.IsFree())
       {
           
           
           rectTransform.anchoredPosition = closestCell.anchoredPosition;
           closestCellComponent.SetOccupied(true);
           currentCell = closestCellComponent;  
           
           if (photonView != null && photonView.IsMine)
           {
               int index = closestCell.GetSiblingIndex();
               photonView.RPC(nameof(RPC_SnapToCellByIndex), RpcTarget.Others, index);
           }
       }
       else
       {
           rectTransform.SetParent(originalParent, worldPositionStays:false);
           rectTransform.anchoredPosition = originalPosition;
       }
   }
   
   [PunRPC]
   void RPC_SnapToCellByIndex(int index)
   {
       if (!gridParent) return;
       if (index < 0 || index >= gridParent.childCount) return;

       var child = gridParent.GetChild(index);
       var targetRT = child as RectTransform;
       var targetCell = child.GetComponent<HexCell>();
       if (!targetRT || targetCell == null) return;

       
       if (!targetCell.IsFree()) return;

       rectTransform.SetParent(gridParent, worldPositionStays:false);
       rectTransform.anchoredPosition = targetRT.anchoredPosition;

       targetCell.SetOccupied(true);
       currentCell = targetCell;
   }
   
}
