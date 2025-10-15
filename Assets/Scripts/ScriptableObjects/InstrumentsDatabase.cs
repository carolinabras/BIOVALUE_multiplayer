using System;
using UnityEngine;

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
    public string name = "New Instrument";
    public string description = "Instrument Description";
    public InstrumentType type = InstrumentType.None;
    public Sprite icon = null;

    public Instrument()
    {
        
    }
    
    public Instrument(string name, string description, InstrumentType type, Sprite icon = null)
    {
        this.name = name;
        this.description = description;
        this.type = type;
        this.icon = icon;
    }
}

[CreateAssetMenu(fileName = "InstrumentsDatabase", menuName = "Scriptable Objects/Instruments Database")]
public class InstrumentsDatabase : ScriptableObject
{
    public Instrument[] instruments;
}