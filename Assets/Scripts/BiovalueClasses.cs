using System;
using Photon.Realtime;
using UnityEngine;

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
        Company = player.CustomProperties.TryGetValue(BiovalueStatics.PlayerCompanyKey, out var c) ? c as string : string.Empty;
        Name = player.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var n) ? n as string : string.Empty;
        Objective = player.CustomProperties.TryGetValue(BiovalueStatics.PlayerObjectiveKey, out var o) ? o as string : string.Empty;
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
public class Instrument
{
    public int id = -1;
    public string name = "New Instrument";
    public string description = "Instrument Description";
    public InstrumentType type = InstrumentType.None;
    public Sprite icon = null;

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
    }
}