using UnityEngine;
using UnityEngine.Rendering;


public class ActionDeck : MonoBehaviour
{
    [SerializeField] private GameObject panelMenuRT;
    [SerializeField] private Transform contentTransform;
    [SerializeField] private GameObject actionCardPrefab;

    [SerializeField] private GameObject buttonPlayCards;
    
    [SerializeField] private float animationTime = 0.5f;
    
    private bool isOpen = false;

    public void OpenMenu()
    {
        if (!isOpen)
        {
            isOpen = true;
            panelMenuRT.SetActive(true);
            buttonPlayCards.SetActive(true);
            LeanTween.scale(panelMenuRT.GetComponent<RectTransform>(), Vector3.one, animationTime).setEaseOutBack();
        }
    }
    
    public void CloseMenu()
    {
        if (isOpen)
        {
            isOpen = false;
            buttonPlayCards.SetActive(false);
            LeanTween.scale(panelMenuRT.GetComponent<RectTransform>(), Vector3.zero, animationTime).setEaseInBack()
                .setOnComplete(() => panelMenuRT.SetActive(false));
        }
    }

}
