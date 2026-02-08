using System;
using UnityEngine;
using UnityEngine.UI;

public class SelectInstrument : MonoBehaviour
{
    
   private InstrumentHook instrumentHook;
   private Image visualImage;   
   private Button button;
   private Image backgroundImage;

   private void Awake()
   {
         instrumentHook = GetComponent<InstrumentHook>();
         button = GetComponentInChildren<Button>(true);

         if (button == null)
         {
             Debug.LogError("Button não encontrado!");
             return;
         }

         // ESTE é o Image que aparece no ecrã
         visualImage = button.GetComponent<Image>();

         // impedir o Button de sobrescrever a cor
         //button.transition = Selectable.Transition.None;

         backgroundImage = GetComponent<Image>();

         //UpdateVisual();
   }

   public void Start()
   {
       if (instrumentHook == null)
       {
           return;
       }
       UpdateVisual();
   }

   public void ToggleSelection()
   {
       var db = InstrumentDatabaseSession.Instance.SessionDb;
       if (db == null)
       {
           Debug.LogError("Session InstrumentsDatabase não inicializada.");
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
       // db.NotifyDatabaseChanged();

       //Debug.Log($"[SelectInstrument] {realInstrument.name} isSelected = {realInstrument.isSelected} (DB HASH={realInstrument.GetHashCode()})");
    
       // if (realInstrument.isSelected == true)
       // {
       //     Debug.Log($"[SelectInstrument] {realInstrument.name} selected");
       //     visualImage.color = Color.green;
       //     backgroundImage.color = Color.green;
       //     
       // }
       
       UpdateVisual();
   }
   
   public void UpdateVisual()
   {
       if (visualImage == null) return;

       visualImage.color =
           instrumentHook.instrument.isSelected ? Color.green : Color.white;
   }
   
   public void ChangeColor()
   {
     //change button color when "selected" is true
       Image backgroundImage = GetComponent<Image>();
       if (backgroundImage != null)
       {
           backgroundImage.color = instrumentHook.instrument.isSelected ? Color.green : Color.white;
       }
   }

 
}
