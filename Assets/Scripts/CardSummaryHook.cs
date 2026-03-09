using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class CardSummaryHook : MonoBehaviourPun
{
    [SerializeField] private Transform ownerContainer;
    [SerializeField] private Transform collabContainer;
    [SerializeField] private GameObject ownerNamePrefab;
    [SerializeField] private GameObject collabNamePrefab;

    public void Setup(int actorNumber, int cardIndex)
    {
        // owner
        GameObject ownerObj = Instantiate(ownerNamePrefab, ownerContainer);
        TMP_Text ownerText = ownerObj.GetComponentInChildren<TMP_Text>();
        if (ownerText != null)
        {
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player.ActorNumber == actorNumber)
                {
                    if (player.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var nameObj))
                        ownerText.text = nameObj as string;
                    else
                        ownerText.text = $"Player {actorNumber}";
                    break;
                }
            }
        }

        // colaborações
        List<int> collaborators = ColaborationManager.Instance.GetCollaborators(actorNumber, cardIndex);
        foreach (int collaboratorActorNumber in collaborators)
        {
            GameObject collabObj = Instantiate(collabNamePrefab, collabContainer);
            CollaboratorHook collabHook = collabObj.GetComponent<CollaboratorHook>();
            if (collabHook != null)
            {
                string collaboratorName = $"Player {collaboratorActorNumber}";
                foreach (var player in PhotonNetwork.PlayerList)
                {
                    if (player.ActorNumber == collaboratorActorNumber)
                    {
                        if (player.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var nameObj))
                            collaboratorName = nameObj as string;
                        break;
                    }
                }

                collabHook.Setup(collaboratorName);
            }
        }
    }
}
            
        

