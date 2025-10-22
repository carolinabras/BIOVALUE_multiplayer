using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InstrumentsDatabase", menuName = "Scriptable Objects/Instruments Database")]
public class InstrumentsDatabase : ScriptableObject
{
    public List<Instrument> instruments;

    public Instrument GetInstrumentById(int id)
    {
        return instruments.Find(instrument => instrument.id == id);
    }
}