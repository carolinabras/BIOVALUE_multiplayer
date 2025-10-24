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

    public void SpawnPlayedActionCardsInNetwork()
    {
        foreach (var actionCard in actionCardsDatabase.actionCards)
        {
            if (actionCard.isPlayed)
            {
                // Photon does not allow us to easily specify the parent of a spawned object.
                // To do that, we need to pass the View ID (from the Photon View component) of the parent object as initialization data.
                // Then, in the spawned object's script (in our case SetPhotonParentOnInstantiation), we can set the parent using that View ID.

                // So first, we get the Photon View component from the parent object.
                PhotonView containerPhotonView = newpanelParent.GetComponent<PhotonView>();
                if (!containerPhotonView)
                {
                    Debug.LogError("Container does not have a PhotonView component.");
                    break;
                }

                // Next, we get the View ID of the parent object and prepare the initialization data.
                int parentViewID = containerPhotonView.ViewID;
                object[] initData = new object[1];
                initData[0] = parentViewID;

                // Now we can instantiate the action card prefab over the network, passing the initialization data.
                GameObject cardObject = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", actionCardPrefab.name),
                    Vector3.zero, Quaternion.identity, 0, initData);

                // We are assuming that the prefab contains the SetPhotonParentOnInstantiation script to handle setting the parent.
                // So we check for that component and log a warning if it's missing.
                if (!cardObject.GetComponent<SetPhotonParentOnInstantiation>())
                {
                    Debug.LogWarning(
                        "Prefab does not have SetPhotonParentOnInstantiation component. Setting the parent is not possible.");
                }

                // If all goes well, we set the action card data on the spawned object.
                ActionCardsHook hook = cardObject.GetComponent<ActionCardsHook>();
                if (hook != null)
                {
                    hook.SetActionCardInNetwork(actionCard);
                }
                
                // NOTE: The newpanelParent is disabled in the scene. This means that its ViewID is not registered in the Photon network.
                // Therefore, cards that should be placed as children of newpanelParent will be ignored until after the parent is activated.
            }
        }
    }
}