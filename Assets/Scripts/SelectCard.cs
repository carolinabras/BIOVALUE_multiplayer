using UnityEngine;

// Kept for old button wiring: selection (limit, highlight) now lives in ActionCardsHook.
public class SelectCard : MonoBehaviour
{
   private ActionCardsHook actionCardsHook;

   public void Awake()
   {
      actionCardsHook = GetComponent<ActionCardsHook>();
   }

   public void SelectThisCard()
   {
      if (actionCardsHook) actionCardsHook.ToggleSelected();
   }
}
