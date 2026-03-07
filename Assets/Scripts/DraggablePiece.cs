using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]


public class DraggablePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Canvas canvas;
    private RectTransform rect;
    private CanvasGroup cg;
    private Transform originalParent;
    private PhotonView pv;
    private Transform dragArea; // camada onde as peças ficam enquanto são arrastadas (para desenhar por cima de tudo)


    [HideInInspector] public bool placedInCell = false;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        cg   = GetComponent<CanvasGroup>();
        pv   = GetComponent<PhotonView>();
        canvas = GetComponentInParent<Canvas>();

        dragArea = GameObject.Find("Board").transform;
        Debug.Log("Drag area: " + dragArea);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        
        // se esta peça tem Tag "GM", só o GM pode mexer
        if (CompareTag("GM") && !IAmGM())
        {
            // cancela o drag
            eventData.pointerDrag = null;
            return;
        }

        if (!pv.IsMine) pv.RequestOwnership();

        originalParent = this.transform;
        placedInCell = false;

        cg.blocksRaycasts = false;          // deixar o raycast chegar às células
        // transform.SetParent(canvas.transform); // desenhar por cima de tudo
        // Debug.Log("originalParent: " + originalParent);
        transform.SetParent(dragArea, worldPositionStays: true);
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!pv.IsMine || eventData.pointerDrag == null) return;
        rect.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        cg.blocksRaycasts = true;

        // se não foi largada numa célula, volta à origem (só localmente)
        if (!placedInCell)
        {
            // transform.SetParent(originalParent);
            // rect.anchoredPosition = Vector2.zero;
            transform.SetParent(originalParent, worldPositionStays: false);
            rect.anchoredPosition = Vector2.zero;
        }
    }

    // chamado pela célula via RPC para TODOS (buffered) → fica igual em todos os clientes
    [PunRPC]
    public void PlaceInCell(int q, int r)
    {
        var cell = HexMapController.Instance?.GetCell(q, r);
        if (cell == null) return;

        transform.SetParent(cell.transform);
        rect.anchoredPosition = Vector2.zero;
        placedInCell = true;
    }

    // usa a tua própria verificação se já tens isto noutro sítio
    private bool IAmGM()
    {
        return Photon.Pun.PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("role", out var v)
               && v is string s && s == "GM";
    }
}