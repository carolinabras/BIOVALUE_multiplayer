using UnityEngine;

public class OpenThisPanel : MonoBehaviour
{
    public GameObject panel;

    public void OpenPanel()
    {
        if (panel == null || panel.activeSelf) return;
        LeanTween.cancel(panel);
        panel.SetActive(true);
        panel.GetComponent<RectTransform>().localScale = Vector3.zero;
        LeanTween.scale(panel.GetComponent<RectTransform>(), Vector3.one, 0.25f)
            .setEaseOutBack()
            .setIgnoreTimeScale(true);
    }
}
