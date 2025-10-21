using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class ActionCardsSpawner : MonoBehaviour
{
    [SerializeField] private GameObject actionCardPrefab;
    [SerializeField] private ActionCardsDatabase actionCardsDatabase;
    [SerializeField] private GameObject parentOfInstruments;
    
    [SerializeField] private Transform panelParent;
    [SerializeField] private Transform newpanelParent;

    [HideInInspector] public List<ActionCardsHook> injectionStepHooks = new List<ActionCardsHook>();

    private void Start()
    {
        Invoke(nameof(SpawnActionCards), 2.0f);
    }

   /* public void Populate()
    {

        if (!actionCardsDatabase)
        {
            Debug.LogError("ActionCards Database is not assigned.");
            return;
        }

        if (!actionCardPrefab)
        {
            Debug.LogError("Protocol category or entry prefab is not assigned.");
            return;
        }

        if (!parentOfInstruments)
        {
            parentOfInstruments = this.gameObject;
        }

        injectionStepHooks =
            UiUtils.FillContainerWithPrefab<ActionCardsHook>(parentOfInstruments, actionCardPrefab,
                actionCardsDatabase.actionCards.Length, (hook, i) =>
                {
                    if (i >= actionCardsDatabase.actionCards.Length)
                    {
                        Debug.LogError(
                            $"Expected {actionCardsDatabase.actionCards.Length} actionCards, but more {i} were provided.");
                        return false;
                    }

                    var card = actionCardsDatabase.actionCards[i];

                    hook.SetActionCard(card);
                    
                    RectTransform hookRect = hook.GetComponent<RectTransform>();
                    if (hookRect)
                    {
                        hookRect.localPosition = new Vector3(50, 50);
                    }

                    return true;
                }, false, false);
    }
    
   */ 
   public void SpawnActionCards()
    {
        foreach (var actionCard in actionCardsDatabase.actionCards)
        {
            GameObject cardObject = Instantiate(actionCardPrefab, panelParent);
            ActionCardsHook hook = cardObject.GetComponent<ActionCardsHook>();
            if (hook != null)
            {
                hook.SetActionCard(actionCard);
            }
        }
    }
   
   public void SpawnPlayedActionCards() 
   {
       foreach (var actionCard in actionCardsDatabase.actionCards)
       {
           if (actionCard.isPlayed)
           {
               GameObject cardObject = Instantiate(actionCardPrefab, newpanelParent);
               ActionCardsHook hook = cardObject.GetComponent<ActionCardsHook>();
               if (hook != null)
               {
                   hook.SetActionCard(actionCard);
               }
           }
       }
   }
    
}
