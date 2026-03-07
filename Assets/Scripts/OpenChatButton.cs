using UnityEngine;

public class OpenChatButton : MonoBehaviour
{
    
    [SerializeField] private RectTransform chatPanel;
    
    [SerializeField] private RectTransform chatPanelWithoutButton;
    [SerializeField] private float openX;
    
    private Vector2 closedPosition;
    private Vector2 openedPosition;

    public bool isOpen;

    public void Awake()
    {
        if (!chatPanel) return;
       
        openX  = chatPanelWithoutButton.rect.width;
        closedPosition = chatPanel.anchoredPosition;
        openedPosition = new Vector2(closedPosition.x - openX, chatPanel.anchoredPosition.y);
            
        
    }
    
    public void ToggleChatPanel()
    {
        if (chatPanel == null) return;
       
        isOpen = !isOpen;
        Vector2 target = isOpen ? openedPosition : closedPosition;

        LeanTween.move(chatPanel, target, 0.25f).setEaseOutCubic();
        
    }

   
   
}
