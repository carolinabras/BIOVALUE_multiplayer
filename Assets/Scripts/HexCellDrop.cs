using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;

public class HexCellDrop : MonoBehaviour, IDropHandler
{
    // definido pelo HexMapController quando gera a grelha
    public Vector2Int axialCoords;

    public void OnDrop(PointerEventData eventData)
    {
        var dropped = eventData.pointerDrag;
        if (dropped == null) return;

        // célula ocupada? (1º filho que não seja UI "decorativo")
        if (HasPiece())
        {
            // opcional: feedback de célula ocupada
            return;
        }

        var pv = dropped.GetComponent<PhotonView>();
        if (pv == null) return;

        // coloca a peça nesta célula para TODOS (Buffered = novos clientes também veem)
        pv.RPC("PlaceInCell", RpcTarget.AllBuffered, axialCoords.x, axialCoords.y);

        // marca localmente que foi colocada (para não voltar à origem no OnEndDrag)
        var drag = dropped.GetComponent<DraggablePiece>();
        if (drag) drag.placedInCell = true;
    }

    private bool HasPiece()
    {
        // considera peça qualquer filho que tenha DraggablePiece
        for (int i = 0; i < transform.childCount; i++)
            if (transform.GetChild(i).GetComponent<DraggablePiece>() != null)
                return true;
        return false;
    }
}