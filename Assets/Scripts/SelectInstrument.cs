using System;
using UnityEngine;
using UnityEngine.UI;

public class SelectInstrument : MonoBehaviour
{
    
   private InstrumentHook instrumentHook;

   private void Awake()
   {
         instrumentHook = GetComponent<InstrumentHook>();
   }

   public void ToggleSelection()
   {
       var db = GameKnowledge.Instance?.instrumentsDatabase;
       if (db == null)
       {
           Debug.LogError("InstrumentsDatabase não encontrada em GameKnowledge.");
           return;
       }

       
       Instrument uiInstrument = instrumentHook.instrument;   // cópia
       Instrument realInstrument = db.GetInstrumentById(uiInstrument.id);

       if (realInstrument == null)
       {
           Debug.LogError($"Instrumento com id {uiInstrument.id} não encontrado na database.");
           return;
       }

       bool newValue = !realInstrument.isSelected;
       realInstrument.isSelected = newValue;

       uiInstrument.isSelected = newValue;

       Debug.Log($"[SelectInstrument] {realInstrument.name} isSelected = {realInstrument.isSelected} (DB HASH={realInstrument.GetHashCode()})");

       UpdateVisual();
   }
   
   public void UpdateVisual()
   {
       if (instrumentHook == null || instrumentHook.instrument == null) return;

       // cores mudar
       Image backgroundImage = GetComponent<Image>();
       if (backgroundImage != null)
       {
           backgroundImage.color = instrumentHook.instrument.isSelected ? Color.green : Color.white;
       }
   }

 
}
