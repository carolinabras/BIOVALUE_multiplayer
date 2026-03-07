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
    
    public ActionCard GetActionCardById(int id)
    {
        return actionCards.Find(actionCard => actionCard.id == id);
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