using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;


public class ActionDeck : MonoBehaviour
{
    [SerializeField] private GameObject panelMenu;


    [SerializeField] private GameObject buttonPlayCards;
    
    [SerializeField] private TMP_Text playedCardsText;

    public List<ActionCard> playedCards;
    public List<int> playedCardIds;
    

    
    [SerializeField] private float animationTime = 0.5f;
    
    private bool isOpen = false;
    
    [SerializeField] private ActionCardsDatabase actionCardsDatabase;
    public void Awake()
    {
        actionCardsDatabase = ActionCardsDatabaseSession.Instance.SessionDb;
    }

    public void PlaySelectedCardsButton()
    {
        actionCardsDatabase.PlaySelectedActionCards();
        playedCards = actionCardsDatabase.GetAllSelectedActionCards();
        foreach (var actionCard in playedCards)
        {
            int id = actionCard.id;
            playedCardIds.Add(id);
        }

        int localPlayerId = GameState.Instance.localPlayerIndex;
        GameState.Instance.SetPlayerActionCards(localPlayerId, playedCardIds);
        
        
        CloseMenu();
        
            int playedCardsCount = actionCardsDatabase.GetSelectedCount();
            playedCardsText.text = $"You played {playedCardsCount} action cards";
        //wait for 5 seconds and then clear the text
        Invoke(nameof(ClearPlayedCardsText), 5f);
        
    }

    public void ClearPlayedCardsText()
    {
        Vector2 target = new Vector2(playedCardsText.rectTransform.anchoredPosition.x + 1000000, playedCardsText.rectTransform.anchoredPosition.y);
        LeanTween.move(playedCardsText.GameObject(), target, 0.25f).setEaseOutCubic();
    }

    public void OpenMenu()
    {
        if (!isOpen)
        {
           
            isOpen = true;
            panelMenu.SetActive(true);
            buttonPlayCards.SetActive(true);
            LeanTween.scale(panelMenu.GetComponent<RectTransform>(), Vector3.one, animationTime).setEaseOutBack();
        }
    }
    
    public void CloseMenu()
    {
        if (isOpen)
        {
          
            isOpen = false;
            buttonPlayCards.SetActive(false);
            LeanTween.scale(panelMenu.GetComponent<RectTransform>(), Vector3.zero, animationTime).setEaseInBack()
                .setOnComplete(() => panelMenu.SetActive(false));
        }
    }



    
    
}
