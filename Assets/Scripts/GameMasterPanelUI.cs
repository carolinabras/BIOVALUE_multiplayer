using System;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class GameMasterPanelUI : MonoBehaviourPunCallbacks
{
   
    public RectTransform panel;

    
   
    
    public float visibleWidth = 80f;

    public float animTime = 0.4f;
    public LeanTweenType easeType = LeanTweenType.easeOutCubic;

    private bool isOpen = false;
    private Vector2 shownPos;
    private Vector2 hiddenPos;

    private void Start()
    {
        //checks if is GM, if not, deactivate panel
        if (!PhotonNetwork.IsMasterClient)
        {
            panel.gameObject.SetActive(false);
        }
        
    }

    private void Awake()
    {
        if (panel == null)
            panel = GetComponent<RectTransform>();

       
        shownPos = panel.anchoredPosition;

        float width = panel.rect.width;

        
        hiddenPos = shownPos + new Vector2(-(width - visibleWidth), 0);

       
        panel.anchoredPosition = hiddenPos;
        isOpen = false;
    }

    public void TogglePanel()
    {
        isOpen = !isOpen;

        Vector2 target = isOpen ? shownPos : hiddenPos;

        LeanTween.cancel(panel);
        LeanTween.move(panel, target, animTime)
            .setEase(easeType);
    }
}
