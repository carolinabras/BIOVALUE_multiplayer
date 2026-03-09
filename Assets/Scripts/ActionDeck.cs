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
    [SerializeField] private GameObject panelMenuMaster;

    
    [SerializeField] private float animationTime = 0.5f;
    
    private bool isOpen = false;
    
    [SerializeField] private ActionCardsDatabase actionCardsDatabase;
    [SerializeField] private ActionCardsSpawner actionCardsSpawner;

    public void PlaySelectedCardsButton()
    {
        playedCards.Clear();
        playedCardIds.Clear();

        int localPlayerId = GameState.Instance.localPlayerIndex;

        foreach (var hook in actionCardsSpawner.spawnedHooks)
        {
            if (hook.IsSelected)
            {
                playedCards.Add(hook.actionCard);
                playedCardIds.Add(hook.actionCard.id);

                if (!string.IsNullOrEmpty(hook.actionCard.descriptionGeneral))
                    GameState.Instance.SetActionCardDescription(localPlayerId, hook.actionCard.id, hook.actionCard.descriptionGeneral);
                if (!string.IsNullOrEmpty(hook.actionCard.descriptionHow))
                    GameState.Instance.SetActionCardDescriptionHow(localPlayerId, hook.actionCard.id, hook.actionCard.descriptionHow);
            }
        }

        if (playedCardIds.Count == 0)
        {
            Debug.LogWarning("Nenhuma carta selecionada!");
            return;
        }

        GameState.Instance.SetPlayerActionCards(localPlayerId, playedCardIds);

        CloseMenu();

        playedCardsText.text = $"You played {playedCardIds.Count} action cards";
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
            panelMenuMaster.SetActive(true);
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
            panelMenuMaster.SetActive(false);
            buttonPlayCards.SetActive(false);
            LeanTween.scale(panelMenu.GetComponent<RectTransform>(), Vector3.zero, animationTime).setEaseInBack()
                .setOnComplete(() => panelMenu.SetActive(false));
        }
    }



    
    
}
