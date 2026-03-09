using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class LinkHandler : MonoBehaviour
{
    [SerializeField] private TMP_Text tmpText;

    public void OnPointerClick(PointerEventData eventData)
    {
        Camera cam = null;
        Canvas canvas = tmpText.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(tmpText, eventData.position, cam);
        Debug.Log($"LinkIndex: {linkIndex}, position: {eventData.position}");
        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = tmpText.textInfo.linkInfo[linkIndex];
            string url = linkInfo.GetLinkID();
            Debug.Log($"Abrir URL: {url}");
            Application.OpenURL(url);
        }
    }
}
