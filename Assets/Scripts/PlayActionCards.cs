using UnityEngine;

public class PlayActionCards : MonoBehaviour
{
    [SerializeField] private Transform actionCardsParent;
    [SerializeField] private GameObject actionCardsPlayedPanel;
    
    
    public void PlaySelectedCards()
    {
        foreach (Transform cardTransform in actionCardsParent)
        {
            ActionCardsHook cardHook = cardTransform.GetComponent<ActionCardsHook>();
            if (cardHook != null && cardHook.actionCard.isSelected && !cardHook.actionCard.isPlayed)
            {
                // Mark the card as played
                cardHook.actionCard.isPlayed = true;
                

                // Optionally, you can add visual feedback or animations here
                Debug.Log($"Played Action Card: {cardHook.actionCard.id}");
            }
        }
        
        actionCardsPlayedPanel.SetActive(true);
    }
}
