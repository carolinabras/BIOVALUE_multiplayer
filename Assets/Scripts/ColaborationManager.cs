using Photon.Pun;
using UnityEngine;

public class ColaborationManager : MonoBehaviourPun
{
    public static ColaborationManager Instance;

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
    
        
        PlayedCardsSpawner spawner = FindSpawnerByOwner(cardOwnerActorNumber);
        Debug.Log($"Spawner encontrado: {spawner != null}, spawnedCards count: {spawner?.spawnedCards.Count}");

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
        PlayedCardsSpawner spawner = FindSpawnerByOwner(cardOwnerActorNumber);
        if (spawner == null)
        {
            Debug.LogWarning($"RPC_HideCollaboration: spawner não encontrado para owner {cardOwnerActorNumber}");
            return;
        }

        if (cardIndex < 0 || cardIndex >= spawner.spawnedCards.Count)
        {
            Debug.LogWarning($"RPC_HideCollaboration: cardIndex {cardIndex} inválido");
            return;
        }

        GameObject card = spawner.spawnedCards[cardIndex];
        card.GetComponent<CollaborateButton>()?.HideCollab(collaboratorActorNumber);
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
