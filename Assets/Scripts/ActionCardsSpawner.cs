using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

    private void Awake()
    {
        actionCardsDatabase = ActionCardsDatabaseSession.Instance.SessionDb;
    }
    
    
    private void Start()
    {
        //Invoke(nameof(SpawnActionCards), 3.0f);
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
    public List<ActionCardsHook> spawnedHooks = new List<ActionCardsHook>();

    public void SpawnActionCards()
    {
        
        spawnedHooks.Clear();
        UiUtils.ClearChildren(panelParent.gameObject);
        foreach (var actionCard in actionCardsDatabase.actionCards)
        {
            ActionCard cardCopy = new ActionCard
            {
                id = actionCard.id,
                cardName = actionCard.cardName,
                descriptionGeneral = actionCard.descriptionGeneral,
                descriptionHow = actionCard.descriptionHow,
                type = actionCard.type,
                icon = actionCard.icon
            };

            GameObject cardObject = Instantiate(actionCardPrefab, panelParent);
            ActionCardsHook hook = cardObject.GetComponent<ActionCardsHook>();
            if (hook != null)
            {
                hook.ownerPlayerId = GameState.Instance.localPlayerIndex;
                hook.SetActionCard(cardCopy);
                spawnedHooks.Add(hook);
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

    public void SpawnPlayedActionCardsInNetwork()
    {
        foreach (var actionCard in actionCardsDatabase.actionCards)
        {
            if (actionCard.isPlayed)
            {
               
                PhotonView containerPhotonView = newpanelParent.GetComponent<PhotonView>();
                if (!containerPhotonView)
                {
                    Debug.LogError("Container does not have a PhotonView component.");
                    break;
                }

                
                int parentViewID = containerPhotonView.ViewID;
                object[] initData = new object[1];
                initData[0] = parentViewID;

                GameObject cardObject = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", actionCardPrefab.name),
                    Vector3.zero, Quaternion.identity, 0, initData);

               
                if (!cardObject.GetComponent<SetPhotonParentOnInstantiation>())
                {
                    Debug.LogWarning(
                        "Prefab does not have SetPhotonParentOnInstantiation component. Setting the parent is not possible.");
                }

                ActionCardsHook hook = cardObject.GetComponent<ActionCardsHook>();
                if (hook != null)
                {
                    hook.SetActionCardInNetwork(actionCard);
                }
                
               
            }
        }
    }
}