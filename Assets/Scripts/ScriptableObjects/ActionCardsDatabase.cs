using System;
using JetBrains.Annotations;
using UnityEngine;

[Serializable]
public enum ActionCardType
{
    none = 0,
    PreDone = 1,
    Custom = 2
}

[Serializable]
public class ActionCard
{
    public string cardName = "New Card";
    public string descriptionGeneral ="Action Card Description";
    public string descriptionHow = "Detailed Description";
    public ActionCardType type = ActionCardType.none;
    public Sprite icon = null;
    public int id = 0;
    
    public bool isSelected = false;
    public bool isPlayed = false;
    
    
    public ActionCard()
    {
        
    }

    public ActionCard(string name, string description, string descriptionHow, ActionCardType type, Sprite sprite,
        int id)
    {
        this.cardName = name;
        this.descriptionGeneral = description;
        this.descriptionHow = descriptionHow;
        this.type = type;
        this.icon = sprite;
        this.id = id;
        
    }
    
}



[CreateAssetMenu(fileName = "ActionCardsDatabase", menuName = "Scriptable Objects/ActionCardsDatabase")]
public class ActionCardsDatabase : ScriptableObject
{
    public ActionCard[] actionCards;
}
