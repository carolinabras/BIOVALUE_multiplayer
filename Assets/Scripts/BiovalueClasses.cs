using System;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class BiovalueStatics
{
    public const string PlayerCompanyKey = "card_company";
    public const string PlayerNameKey = "card_name";
    public const string PlayerObjectiveKey = "card_objective";
    public const string PlayerReadyKey = "card_ready";
}

public class BiovaluePlayer
{
    public Player Player;
    public string Company;
    public string Name;
    public string Objective;

    public bool IsGameMaster => Player?.IsMasterClient ?? false;

    public int ActorNumber => Player?.ActorNumber ?? -1;

    public BiovaluePlayer()
    {
    }

    public BiovaluePlayer(Player player)
    {
        Player = player;
        if (player == null) return;

        Company = player.CustomProperties.TryGetValue(BiovalueStatics.PlayerCompanyKey, out var c)
            ? c as string
            : string.Empty;
        Name = player.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var n)
            ? n as string
            : string.Empty;
        Objective = player.CustomProperties.TryGetValue(BiovalueStatics.PlayerObjectiveKey, out var o)
            ? o as string
            : string.Empty;
    }
}

[Serializable]
public enum InstrumentType
{
    None = 0,
    Fiscalization = 1,
    Regulamentation = 2,
    Accord = 3
}

[Serializable]
public class Objective
{
    public int id = -1;
    public string name = "New Objective";
    public string description = "Objective Description";
    public bool isSelected = false;
    
    public Objective()
    {
    }
    
    public Objective(Objective objective)
    {
        this.id = objective.id;
        this.name = objective.name;
        this.description = objective.description;
        this.isSelected = objective.isSelected;
    }
}

[Serializable]
public class Instrument
{
    public int id = -1;
    public string name = "New Instrument";
    public string description = "Instrument Description";
    public InstrumentType type = InstrumentType.None;
    public Sprite icon = null;
    public string generalDescription = "General Description of the Instrument";
    public bool isSelected = false;
    
    
    public Instrument()
    {
    }

    public Instrument(Instrument instrument)
    {
        this.id = instrument.id;
        this.name = instrument.name;
        this.description = instrument.description;
        this.type = instrument.type;
        this.icon = instrument.icon;
        this.isSelected = instrument.isSelected;
        
    }
}


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
    public string descriptionGeneral = "Action Card Description";
    public string descriptionHow = "Detailed Description";
    public ActionCardType type = ActionCardType.none;
    public Sprite icon = null;
    public int id = 0;

    public bool isSelected = false;
    public bool isPlayed = false;


    public ActionCard()
    {
    }

    public ActionCard(ActionCard card)
    {
        this.cardName = card.cardName;
        this.descriptionGeneral = card.descriptionGeneral;
        this.descriptionHow = card.descriptionHow;
        this.type = card.type;
        this.icon = card.icon;
        this.id = card.id;
        this.isSelected = card.isSelected;
        this.isPlayed = card.isPlayed;
    }
}