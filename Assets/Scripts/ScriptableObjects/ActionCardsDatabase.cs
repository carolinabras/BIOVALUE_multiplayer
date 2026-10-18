using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


[CreateAssetMenu(fileName = "ActionCardsDatabase", menuName = "Scriptable Objects/ActionCardsDatabase")]
public class ActionCardsDatabase : ScriptableObject
{
    public List<ActionCard> actionCards;
    
    public UnityEvent OnDatabaseChanged;
    
    public void NotifyDatabaseChanged()
    {
        OnDatabaseChanged?.Invoke();
    }
    
    // Empty (player-written) cards are created at runtime with ids starting here,
    // so they never collide with the pre-written cards stored in this asset.
    public const int CustomCardIdStart = 1000;

    public ActionCard GetActionCardById(int id)
    {
        ActionCard card = actionCards.Find(actionCard => actionCard.id == id);
        if (card == null && id >= CustomCardIdStart)
            card = CreateCustomCard(id);
        return card;
    }

    // Builds a blank, writable card. Every client can rebuild it from the id alone;
    // the text the player wrote is synced separately through GameState.
    public ActionCard CreateCustomCard(int id)
    {
        ActionCard template = actionCards.Find(c => c != null && c.type == ActionCardType.Custom);
        return new ActionCard
        {
            id = id,
            cardName = $"Open Action {id - CustomCardIdStart + 1}",
            descriptionGeneral = "",
            descriptionHow = "",
            type = ActionCardType.Custom,
            icon = template?.icon
        };
    }
    
    public void AddActionCard(ActionCard actionCard)
    {
        actionCards.Add(actionCard);
        NotifyDatabaseChanged();
    }

    public List<ActionCard> GetAllSelectedActionCards()
    {        List<ActionCard> selectedActionCards = new List<ActionCard>();
        
        foreach (var actionCard in actionCards)
        {
            if (actionCard != null && actionCard.isSelected)
                selectedActionCards.Add(actionCard);
        }

        return selectedActionCards;
    }
    
    public void PlaySelectedActionCards()
    {
        foreach (var actionCard in actionCards)
        {
            if (actionCard != null && actionCard.isSelected)
            {
                actionCard.isPlayed = true;
            }
        }
        
        NotifyDatabaseChanged();
    }
    
    
    
    public int GetSelectedCount()
    {
        int count = 0;

        foreach (var actionCard in actionCards)
        {
            if (actionCard != null && actionCard.isSelected)
                count++;
        }

        return count;
    }
    
    public void GetSelectedActionCards(List<ActionCard> selectedActionCards)
    {
        selectedActionCards.Clear();
        
        foreach (var actionCard in actionCards)
        {
            if (actionCard != null && actionCard.isSelected)
                selectedActionCards.Add(actionCard);
        }
    }
    
    
}