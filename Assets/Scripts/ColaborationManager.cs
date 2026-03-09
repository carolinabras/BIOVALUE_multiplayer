using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ColaborationManager : MonoBehaviourPun
{
    public static ColaborationManager Instance;

    public Dictionary<string, List<int>> collaborations = new Dictionary<string, List<int>>();

    private void Awake()
    {
        Instance = this;
    }

    public void SendCollaboration(int collaboratorActorNumber, int cardOwnerActorNumber, int cardIndex)
    {
        photonView.RPC(nameof(RPC_ShowCollaboration), RpcTarget.All, collaboratorActorNumber, cardOwnerActorNumber, cardIndex);
    }

    public void SendRemoveCollaboration(int collaboratorActorNumber, int cardOwnerActorNumber, int cardIndex)
    {
        photonView.RPC(nameof(RPC_HideCollaboration), RpcTarget.All, collaboratorActorNumber, cardOwnerActorNumber, cardIndex);
    }

    [PunRPC]
    private void RPC_ShowCollaboration(int collaboratorActorNumber, int cardOwnerActorNumber, int cardIndex)
    {
        Debug.Log($"RPC_ShowCollaboration recebido! collaborator={collaboratorActorNumber}, owner={cardOwnerActorNumber}, index={cardIndex}");

        // guarda em memória
        string key = $"{cardOwnerActorNumber}_{cardIndex}";
        if (!collaborations.ContainsKey(key))
            collaborations[key] = new List<int>();
        if (!collaborations[key].Contains(collaboratorActorNumber))
            collaborations[key].Add(collaboratorActorNumber);

        PlayedCardsSpawner spawner = FindSpawnerByOwner(cardOwnerActorNumber);

        if (spawner == null)
        {
            Debug.LogWarning($"RPC_ShowCollaboration: spawner não encontrado para owner {cardOwnerActorNumber}");
            return;
        }

        if (cardIndex < 0 || cardIndex >= spawner.spawnedCards.Count)
        {
            Debug.LogWarning($"RPC_ShowCollaboration: cardIndex {cardIndex} inválido");
            return;
        }

        GameObject card = spawner.spawnedCards[cardIndex];
        card.GetComponent<CollaborateButton>()?.ShowCollab(collaboratorActorNumber);
    }

    [PunRPC]
    private void RPC_HideCollaboration(int collaboratorActorNumber, int cardOwnerActorNumber, int cardIndex)
    {
        // remove da memória
        string key = $"{cardOwnerActorNumber}_{cardIndex}";
        if (collaborations.ContainsKey(key))
            collaborations[key].Remove(collaboratorActorNumber);

        PlayedCardsSpawner spawner = FindSpawnerByOwner(cardOwnerActorNumber);
        if (spawner == null)
        {
            return;
        }

        if (cardIndex < 0 || cardIndex >= spawner.spawnedCards.Count)
        {
            return;
        }

        GameObject card = spawner.spawnedCards[cardIndex];
        card.GetComponent<CollaborateButton>()?.HideCollab(collaboratorActorNumber);
    }

    public List<int> GetCollaborators(int cardOwnerActorNumber, int cardIndex)
    {
        string key = $"{cardOwnerActorNumber}_{cardIndex}";
        return collaborations.TryGetValue(key, out var list) ? list : new List<int>();
    }

    private PlayedCardsSpawner FindSpawnerByOwner(int cardOwnerActorNumber)
    {
        foreach (var spawner in FindObjectsOfType<PlayedCardsSpawner>())
        {
            if (spawner.actorNumber == cardOwnerActorNumber)
                return spawner;
        }
        return null;
    }
}
