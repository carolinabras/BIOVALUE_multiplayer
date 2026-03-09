using System;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class GameMasterPanelUI : MonoBehaviourPunCallbacks
{
    public RectTransform masterPanel;

    public RectTransform parentPanel;
    
    [SerializeField] private float openX;

   
    
    public LeanTweenType easeType = LeanTweenType.easeOutCubic;

    private bool isOpen = false;
    private Vector2 shownPos;
    private Vector2 hiddenPos;

    private void Start()
    {
        if (!PhotonNetwork.IsMasterClient){
            parentPanel.gameObject.SetActive(false);
        }
        
    }

    private void Awake()
    {
        if(!masterPanel) return;
        openX = masterPanel.rect.width;
        hiddenPos = masterPanel.anchoredPosition;
        shownPos = new Vector2(masterPanel.anchoredPosition.x + openX, masterPanel.anchoredPosition.y);
        
    }

    public void TogglePanel()
    {
        isOpen = !isOpen;

        Vector2 target = isOpen ? shownPos : hiddenPos;
        LeanTween.move(masterPanel, target, 0.25f).setEaseOutCubic();

    }
}
