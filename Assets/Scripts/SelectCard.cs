using System;
using UnityEngine;
using UnityEngine.UI;

public class SelectCard : MonoBehaviour
{
   private ActionCardsHook actionCardsHook;
   private Outline outline;

   [SerializeField] private float distance = 10f;
   
   private bool isSelected = false;

   public void Awake()
   {
      actionCardsHook = GetComponent<ActionCardsHook>();
      outline = this.GetComponent<Outline>();
      outline.effectColor = new Color32(255, 255, 255, 255);
   }

   public void SelectThisCard() 
   {
      isSelected = !isSelected;
      actionCardsHook.IsSelected = isSelected;

      if (isSelected)
      {
         outline.effectColor = new Color32(254, 70, 165, 255);
         outline.effectDistance = new Vector2(distance, distance);
      }
      else
      {
         outline.effectColor = new Color32(255, 255, 255, 255);
         outline.effectDistance = new Vector2(0, 0);
      }
   }

   
}
