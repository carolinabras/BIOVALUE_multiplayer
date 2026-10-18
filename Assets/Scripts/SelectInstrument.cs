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
             Debug.LogError("Button not found!");
             return;
         }

         // THIS is the Image shown on screen
         visualImage = button.GetComponent<Image>();

         // On the hexagon pieces the only button is the delete button: don't tint it.
         if (GetComponent<InstrumentPieceView>() != null) visualImage = null;

         // stop the Button from overriding the colour
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
           Debug.LogError("Session InstrumentsDatabase not initialised.");
           return;
       }

       
       Instrument uiInstrument = instrumentHook.instrument;   // copy
       Instrument realInstrument = db.GetInstrumentById(uiInstrument.id);

       if (realInstrument == null)
       {
           Debug.LogError($"Instrument with id {uiInstrument.id} not found in the database.");
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
           var c = instrumentHook.instrument.isSelected ? Color.green : Color.white;
           c.a = backgroundImage.color.a; // keeps an invisible hit area invisible
           backgroundImage.color = c;
       }
   }

 
}
