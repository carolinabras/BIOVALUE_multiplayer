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
       if (instrumentHook == null || instrumentHook.instrument == null) return;

       // toggle no hook
       instrumentHook.instrument.isSelected = !instrumentHook.instrument.isSelected;
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
