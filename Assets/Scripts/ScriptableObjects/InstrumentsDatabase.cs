using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "InstrumentsDatabase", menuName = "Scriptable Objects/Instruments Database")]
public class InstrumentsDatabase : ScriptableObject
{
    public List<Instrument> instruments;

    public UnityEvent OnDatabaseChanged;
    
    
    public void AddInstrument(Instrument instrument)
    {
        instruments.Add(instrument);
        OnDatabaseChanged?.Invoke();
    }
    
    public void RemoveSelectedInstruments()
    {
        instruments.RemoveAll(i => i != null && i.isSelected);
        OnDatabaseChanged?.Invoke();
    }


    
    public void RemoveInstrument(Instrument instrument)
    {
        instruments.Remove(instrument);
        OnDatabaseChanged?.Invoke();
    }
    
    public void NotifyDatabaseChanged()
    {
        OnDatabaseChanged?.Invoke();
    }

    public Instrument GetInstrumentById(int id)
    {
        return instruments.Find(instrument => instrument.id == id);
    }
    
    public int GetSelectedCount()
    {
        int count = 0;

        foreach (var instrument in instruments)
        {
            if (instrument != null && instrument.isSelected)
                count++;
        }

        return count;
    }
    
    /*public List<Instrument> GetSelectedInstruments()
    {
        List<Instrument> selectedInstruments = new List<Instrument>();
        
        return selectedInstruments;
    } */
    
    public List<Instrument> GetSelectedInstruments()
    {
        List<Instrument> selected = new List<Instrument>();
        foreach (var i in instruments)
            if (i != null && i.isSelected) selected.Add(i);
        return selected;
    }
    
    
}