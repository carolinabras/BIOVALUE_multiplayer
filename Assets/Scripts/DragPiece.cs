using System;
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Photon.Pun;


public class DragPiece : MonoBehaviourPun, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /* private Vector2 originalPosition;
    private Transform originalParent;
    public RectTransform rectTransform;
    public Canvas canvas;

    private PhotonView photonView;

    private HexCell currentCell; // the cell where the piece is currently placed

    public RectTransform gridParent;

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        photonView = GetComponent<PhotonView>();

        if (!gridParent && transform.parent != null)
        {
            gridParent = transform.parent as RectTransform;
        }
    }


    // Transform parentAfterDrag;
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!photonView.IsMine)
        {
            photonView.RequestOwnership();
            return;
        }

        if (!rectTransform || !canvas || !photonView || !gridParent)
        {
            Initialize();
        }

        //remember the original parent to return to if not dropped on a valid target
        originalPosition = rectTransform.anchoredPosition;
        originalParent = rectTransform.parent;
        rectTransform.SetParent(gridParent, worldPositionStays: false); // move to be child of grid to align positions
        rectTransform.SetAsLastSibling();

        if (currentCell != null)
        {
            currentCell.SetOccupied(false);
            currentCell = null;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!rectTransform || !canvas || !photonView || !gridParent)
        {
            Initialize();
        }

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
            // Debug.LogError("HexMapController not found in the scene.");
            // rectTransform.anchoredPosition = originalPosition;
             //return;
         //}
        // var cells = HexMapController.Instance.GetAllCells();

         //var gridParent = hmc.GridParent;
         //if (gridParent == null)
        // {
            // Debug.Log("GridParent is not assigned on the HexMapController (drag the MapGrid in the Inspector).");
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
            if (!cell.IsFree()) continue; // ignore occupied cells

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
            rectTransform.SetParent(originalParent, worldPositionStays: false);
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

        rectTransform.SetParent(gridParent, worldPositionStays: false);
        rectTransform.anchoredPosition = targetRT.anchoredPosition;

        targetCell.SetOccupied(true);
        currentCell = targetCell;
    }
    */



    // ////////////////////////////////////////THIS ONE WORKS
    private Vector2 originalPosition;
    private Transform originalParent;


    // teste //////////////////////////////////////////

    private Transform parentAfterDrag;



    [SerializeField] private GameObject closeInstrumentButton;

    public RectTransform rectTransform;

    public GameState _gameState;

    // cell the piece is currently in
    private BoardCell currentCell;
    private int currentCellIndex = -1;


    public GameObject tempBoard;
    public bool canDrag; // can only drag during its owner's turn


    public RectTransform gridParent;



    private void Awake()
    {
        _gameState = GameState.Instance;

        if (closeInstrumentButton) closeInstrumentButton.SetActive(false);

        if (!gridParent) gridParent = FindGrid();
    }

    // The board is the parent of the cells. Found through a cell rather than by the name "Board",
    // which other UI objects can share.
    private static RectTransform FindGrid()
    {
        var cell = FindFirstObjectByType<BoardCell>(FindObjectsInactive.Include);
        if (cell && cell.transform.parent is RectTransform grid) return grid;
        var board = GameObject.Find("Board");
        return board ? board.GetComponent<RectTransform>() : null;
    }

    private void Start()
    {
        if (_gameState == null) return;
        _gameState.onPlayerTurnIndexChanged.AddListener(OnPlayerTurnIndexChanged);
        OnPlayerTurnIndexChanged(_gameState.GetCurrentPlayerTurnIndex());




    }

    public void OnPlayerTurnIndexChanged(int index)
    {
        // Debug/Editor only: let the GM drag pieces regardless of whose turn it
        // is, so drag/board behavior can be tested solo without other players.
        // Never true in a release build.
        bool debugGmOverride = Debug.isDebugBuild && GameAuthority.IsGameMaster;

        canDrag = _gameState.IsMyTurn() || debugGmOverride;
    }


    /*public void Initialize()
    {
        // rectTransform = GetComponent<RectTransform>();
        // canvas = GetComponentInParent<Canvas>();
        // photonView = GetComponent<PhotonView>();
        //
        // if (!gridParent && transform.parent != null)
        // {
        //     gridParent = transform.parent as RectTransform;
        // }


        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        photonView = GetComponent<PhotonView>();

        if (!gridParent)
        {
            var board = GameObject.Find("Board");
            if (board != null)
            {
                gridParent = board.GetComponent<RectTransform>();
            }
            Debug.LogWarning("GridParent not assigned in the Inspector, looking for a GameObject named 'Board'... " + (gridParent ? "Found!" : "Not found!"));
        }
    }*/



    private bool isDragging = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!canDrag) return;
        if (!photonView.IsMine)
        {
            // pede ownership 

            photonView.RequestOwnership();


        }

        GetComponent<SpawnerDetails>()?.CancelLongPress();

        if (closeInstrumentButton) closeInstrumentButton.SetActive(false);

        originalPosition = rectTransform.anchoredPosition;
        originalParent = rectTransform.parent;

        // make the piece a child of the grid so it lines up with the cells
        //rectTransform.SetParent(gridParent, worldPositionStays: false);
        this.transform.SetParent(gridParent.transform);
        this.transform.localScale = Vector3.one; // SetParent (worldPositionStays: true) can rescale on reparent
        this.transform.SetAsLastSibling();
        Debug.LogWarning("the parent of this object is " + this.transform.parent.name);
        isDragging = true;


    }

    public void OnDrag(PointerEventData eventData)
    {

        if (!isDragging) return;
        //rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        transform.position = Input.mousePosition;

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        SnapToClosestCell();
        GetComponent<SpawnerDetails>()?.CancelLongPress();
        Debug.Log("Drag Ended on " + rectTransform.anchoredPosition);

        isDragging = false;
    }

    private void SnapToClosestCell()
    {
        if (!gridParent) return;

        RectTransform closestCellRT = null;
        BoardCell closestCellComponent = null;
        float closestDistance = float.MaxValue;

        Vector2 currentPos = rectTransform.anchoredPosition;

        // find the nearest free cell
        for (int i = 0; i < gridParent.childCount; i++)
        {
            var child = gridParent.GetChild(i);
            var cellRT = child as RectTransform;
            if (!cellRT || cellRT == this.transform) continue; // skip the piece itself

            var cell = child.GetComponent<BoardCell>();
            if (cell == null) continue;

            // skip occupied cells
            if (!cell.IsFree()) continue;

            float distance = Vector2.Distance(cellRT.anchoredPosition, currentPos);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCellRT = cellRT;
                closestCellComponent = cell;
            }
        }

        if (closestCellRT != null && closestCellComponent != null)
        {
            // old and new index (to free and occupy them on the other clients)
            int oldIndex = currentCellIndex;
            int newIndex = closestCellRT.GetSiblingIndex();

            // free the old cell locally
            if (currentCell != null)
            {
                currentCell.SetOccupied(false);
            }

            // occupy the new cell locally
            closestCellComponent.SetOccupied(true);
            currentCell = closestCellComponent;
            currentCellIndex = newIndex;

            // move the piece visually
            rectTransform.SetParent(gridParent, worldPositionStays: false);
            rectTransform.anchoredPosition = closestCellRT.anchoredPosition;
            MatchCellSize(closestCellRT);

            // coming from the hand → piece flip animation
            if (oldIndex < 0) PlayPlacementFlip();

            // show the close button only to the piece's owner
            if (closeInstrumentButton && photonView != null && photonView.IsMine)
                closeInstrumentButton.SetActive(true);

            // sync with the others
            if (photonView != null && photonView.IsMine)
            {
                photonView.RPC(nameof(RPC_SnapToCellByIndex), RpcTarget.Others, newIndex, oldIndex);
                BoardPlacementService.RegisterPlacement(this, newIndex, oldIndex);
            }
        }
        else
        {
            // no valid cell found → go back to the original spot
            rectTransform.SetParent(originalParent, worldPositionStays: false);
            rectTransform.anchoredPosition = originalPosition;
        }
    }

    // Tap-to-place (InstrumentTrayView): the same as a drag that ends exactly on this cell.
    // Ownership is requested first and the placement waits for it, so the result is synced
    // like a normal drag (a drag usually has the ownership by the time it ends).
    public IEnumerator PlaceAtCell(RectTransform cellRT)
    {
        if (!canDrag || !gridParent || !cellRT || isDragging) yield break;

        if (photonView != null && !photonView.IsMine)
        {
            photonView.RequestOwnership();
            float waited = 0f;
            while (!photonView.IsMine && waited < 2f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!photonView.IsMine)
            {
                Debug.LogWarning("[DragPiece] Tap-to-place: ownership not granted, piece not placed.");
                yield break;
            }
        }

        GetComponent<SpawnerDetails>()?.CancelLongPress();
        if (closeInstrumentButton) closeInstrumentButton.SetActive(false);

        originalPosition = rectTransform.anchoredPosition;
        originalParent = rectTransform.parent;

        rectTransform.SetParent(gridParent, worldPositionStays: false);
        rectTransform.SetAsLastSibling();
        MatchCellSize(cellRT); // same anchors as the cell, so the nearest free cell is this one
        rectTransform.anchoredPosition = cellRT.anchoredPosition;
        SnapToClosestCell();
    }

    [PunRPC]
    void RPC_SnapToCellByIndex(int newIndex, int oldIndex)
    {
        if (!gridParent) return;
        if (newIndex < 0 || newIndex >= gridParent.childCount) return;

        // free the old cell on this client
        if (oldIndex >= 0 && oldIndex < gridParent.childCount)
        {
            var oldChild = gridParent.GetChild(oldIndex);
            var oldCell = oldChild.GetComponent<BoardCell>();
            if (oldCell != null)
            {
                oldCell.SetOccupied(false);
            }
        }

        // occupy the new cell
        var child = gridParent.GetChild(newIndex);
        var targetRT = child as RectTransform;
        var targetCell = child.GetComponent<BoardCell>();
        if (!targetRT || targetCell == null) return;

        if (!targetCell.IsFree()) return;

        rectTransform.SetParent(gridParent, worldPositionStays: false);

        rectTransform.anchoredPosition = targetRT.anchoredPosition;
        MatchCellSize(targetRT);

        targetCell.SetOccupied(true);
        currentCell = targetCell;
        currentCellIndex = newIndex;

        if (oldIndex < 0) PlayPlacementFlip();
    }

    private void PlayPlacementFlip()
    {
        var view = GetComponent<InstrumentPieceView>();
        if (view) view.PlayPlacementFlip();
    }

    // Forces this piece's anchor/pivot/size to exactly match the cell it's being
    // placed in. The piece can pick up a different anchor/size on its way here
    // (the spawner's GridLayoutGroup controls both while it's in the spawn list,
    // and reparenting mid-drag can carry that mismatch along) — snapping it back
    // to the cell's own values guarantees it always reads as "one piece, one cell"
    // regardless of what size/anchor state it arrived with.
    private void MatchCellSize(RectTransform cellRT)
    {
        rectTransform.localScale = Vector3.one;
        rectTransform.anchorMin = cellRT.anchorMin;
        rectTransform.anchorMax = cellRT.anchorMax;
        rectTransform.pivot = cellRT.pivot;
        // Cells with the raised-tile look draw a smaller tile ("Top", centred) so tiles don't touch;
        // the piece takes the tile's size so it sits exactly on it.
        var tile = cellRT.Find("Top") as RectTransform;
        rectTransform.sizeDelta = tile ? tile.rect.size : cellRT.sizeDelta;
    }

    // ── Revert ──────────────────────────────────────────────────────────────

    [PunRPC]
    public void RPC_RevertToCell(int oldCellIndex)
    {
        RevertToCell(oldCellIndex);
    }

    public void RevertToCell(int oldCellIndex)
    {
        if (oldCellIndex < 0)
        {
            // Clear current board-cell occupancy on all clients.
            if (currentCell != null)
            {
                currentCell.SetOccupied(false);
                currentCell = null;
                currentCellIndex = -1;
            }

            if (closeInstrumentButton) closeInstrumentButton.SetActive(false);

            // Use the spawn parent (stored in PhotonView.InstantiationData) so this
            // works on every client, not only the one who dragged the piece.
            Transform spawnParent = GetSpawnParent();
            rectTransform.SetParent(spawnParent != null ? spawnParent : originalParent, worldPositionStays: false);
            rectTransform.anchoredPosition = Vector2.zero;

            var instrument = GetComponent<InstrumentHook>()?.instrument;
            if (instrument != null)
            {
                instrument.isPlaced = false;
                InstrumentDatabaseSession.Instance?.SessionDb?.NotifyDatabaseChanged();
            }
            return;
        }

        if (!gridParent || oldCellIndex >= gridParent.childCount) return;

        if (currentCell != null)
            currentCell.SetOccupied(false);

        var oldChild = gridParent.GetChild(oldCellIndex);
        var oldRT = oldChild as RectTransform;
        var oldCell = oldChild.GetComponent<BoardCell>();
        if (oldRT == null || oldCell == null) return;

        rectTransform.SetParent(gridParent, worldPositionStays: false);
        rectTransform.anchoredPosition = oldRT.anchoredPosition;
        MatchCellSize(oldRT);
        oldCell.SetOccupied(true);
        currentCell = oldCell;
        currentCellIndex = oldCellIndex;
    }

    // Reads the spawn-parent transform from the PhotonView instantiation data.
    // This is set by UiUtils.FillContainerWithPrefab and works on every client.
    private Transform GetSpawnParent()
    {
        var pv = GetComponent<PhotonView>();
        if (pv?.InstantiationData != null &&
            pv.InstantiationData.Length > 0 &&
            pv.InstantiationData[0] is int parentViewId)
        {
            var parentPv = PhotonView.Find(parentViewId);
            if (parentPv != null) return parentPv.transform;
        }
        return null;
    }
    
    // Called by the closeInstrumentButton on the prefab.
    public void OnCloseInstrument()
    {
        if (closeInstrumentButton) closeInstrumentButton.SetActive(false);

        rectTransform.SetParent(originalParent, worldPositionStays: false);
        rectTransform.anchoredPosition = originalPosition;

        if (currentCell != null)
        {
            currentCell.SetOccupied(false);
            currentCell = null;
            currentCellIndex = -1;
        }
    }

    public void GiveOwner(int ownerID)
    {
        var instrument = GetComponent<InstrumentHook>()?.instrument;
        if (instrument != null)
        {
            InstrumentDatabaseSession.Instance.SessionDb.SetOwner(ownerID, instrument);
            instrument.isPlaced = true;
            InstrumentDatabaseSession.Instance.SessionDb.NotifyDatabaseChanged();
            Debug.Log("player owner is" + instrument.ownerId);
        }

        photonView.RPC(nameof(RPC_GiveOwner), RpcTarget.Others, ownerID);
    }

    [PunRPC]
    void RPC_GiveOwner(int ownerID)
    {
        var instrument = GetComponent<InstrumentHook>()?.instrument;
        if (instrument == null)
        {
            Debug.LogWarning("RPC_GiveOwner: Instrument not found.");
            return;
        }

        InstrumentDatabaseSession.Instance.SessionDb.SetOwner(ownerID, instrument);
        instrument.isPlaced = true;
        InstrumentDatabaseSession.Instance.SessionDb.NotifyDatabaseChanged();
        Debug.Log(
            $"Owner definido: PlayerIndex {ownerID} → Instrument {instrument.id}, isPlaced: {instrument.isPlaced}");



    }
}