using System;
using System.Collections.Generic;
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

[CreateAssetMenu(fileName = "InstrumentsDatabase", menuName = "Scriptable Objects/Instruments Database")]
public class InstrumentsDatabase : ScriptableObject
{
    public List<Instrument> instruments;

    public Instrument GetInstrumentById(int id)
    {
        return instruments.Find(instrument => instrument.id == id);
    }
}