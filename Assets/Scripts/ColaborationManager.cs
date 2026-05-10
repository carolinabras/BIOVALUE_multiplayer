using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class ColaborationManager : MonoBehaviourPun
{
    public static ColaborationManager Instance;

    [SerializeField] private GameObject collabPopUpPrefab;
    [SerializeField] private Transform popUpParent;

    public Dictionary<string, List<int>> collaborations = new Dictionary<string, List<int>>();

    private void Awake()
    {
        Instance = this;
    }

    // ── Request flow ──────────────────────────────────────────────────────────

    public void RequestCollaboration(int collaboratorActorNumber, int cardOwnerActorNumber, int cardIndex)
    {
        Player targetPlayer = FindPlayer(cardOwnerActorNumber);
        if (targetPlayer == null) return;
        photonView.RPC(nameof(RPC_ShowCollabPopUp), targetPlayer, collaboratorActorNumber, cardOwnerActorNumber, cardIndex);
    }

    [PunRPC]
    private void RPC_ShowCollabPopUp(int collaboratorActorNumber, int cardOwnerActorNumber, int cardIndex)
    {
        string requesterName = GetPlayerName(collaboratorActorNumber);
        string requesterEntity = GetPlayerCompany(collaboratorActorNumber);
        string cardName = GetCardName(cardOwnerActorNumber, cardIndex);

        Transform parent = popUpParent != null ? popUpParent : FindObjectOfType<Canvas>().transform;
        GameObject popUp = Instantiate(collabPopUpPrefab, parent);
        popUp.GetComponent<CollabPopUpHook>()?.Setup(requesterName, requesterEntity, cardName, collaboratorActorNumber, cardOwnerActorNumber, cardIndex);
    }

    // ── Owner response ────────────────────────────────────────────────────────

    public void ConfirmCollaboration(int collaboratorActorNumber, int cardOwnerActorNumber, int cardIndex)
    {
        SendCollaboration(collaboratorActorNumber, cardOwnerActorNumber, cardIndex);

        string ownerName = GetPlayerName(cardOwnerActorNumber);
        string collabName = GetPlayerName(collaboratorActorNumber);
        string cardName = GetCardName(cardOwnerActorNumber, cardIndex);
        GameLog.Instance?.Log($"{ownerName} accepted {collabName}'s collaboration on \"{cardName}\"");

        Player collaborator = FindPlayer(collaboratorActorNumber);
        if (collaborator != null)
            photonView.RPC(nameof(RPC_CollabAccepted), collaborator, collaboratorActorNumber, cardOwnerActorNumber, cardIndex);
    }

    public void DenyCollaboration(int collaboratorActorNumber, int cardOwnerActorNumber, int cardIndex)
    {
        string ownerName = GetPlayerName(cardOwnerActorNumber);
        string collabName = GetPlayerName(collaboratorActorNumber);
        string cardName = GetCardName(cardOwnerActorNumber, cardIndex);
        GameLog.Instance?.Log($"{ownerName} declined {collabName}'s collaboration on \"{cardName}\"");

        Player collaborator = FindPlayer(collaboratorActorNumber);
        if (collaborator != null)
            photonView.RPC(nameof(RPC_CollabDeclined), collaborator, collaboratorActorNumber, cardOwnerActorNumber, cardIndex);
    }

    [PunRPC]
    private void RPC_CollabAccepted(int collaboratorActorNumber, int cardOwnerActorNumber, int cardIndex)
    {
        FindCollaborateButton(cardOwnerActorNumber, cardIndex)?.OnCollabAccepted();
    }

    [PunRPC]
    private void RPC_CollabDeclined(int collaboratorActorNumber, int cardOwnerActorNumber, int cardIndex)
    {
        FindCollaborateButton(cardOwnerActorNumber, cardIndex)?.OnCollabDeclined();
    }

    // ── Existing collaboration sync ───────────────────────────────────────────

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

        spawner.spawnedCards[cardIndex].GetComponent<CollaborateButton>()?.ShowCollab(collaboratorActorNumber);
    }

    [PunRPC]
    private void RPC_HideCollaboration(int collaboratorActorNumber, int cardOwnerActorNumber, int cardIndex)
    {
        string key = $"{cardOwnerActorNumber}_{cardIndex}";
        if (collaborations.ContainsKey(key))
            collaborations[key].Remove(collaboratorActorNumber);

        PlayedCardsSpawner spawner = FindSpawnerByOwner(cardOwnerActorNumber);
        if (spawner == null) return;
        if (cardIndex < 0 || cardIndex >= spawner.spawnedCards.Count) return;

        spawner.spawnedCards[cardIndex].GetComponent<CollaborateButton>()?.HideCollab(collaboratorActorNumber);
    }

    public List<int> GetCollaborators(int cardOwnerActorNumber, int cardIndex)
    {
        string key = $"{cardOwnerActorNumber}_{cardIndex}";
        return collaborations.TryGetValue(key, out var list) ? list : new List<int>();
    }

    // How many accepted collaboration requests this actor made on other players' cards.
    public int GetCollaborationsDone(int actorNumber)
    {
        int count = 0;
        foreach (var list in collaborations.Values)
            if (list.Contains(actorNumber)) count++;
        return count;
    }

    // How many accepted collaborators joined this actor's own cards.
    public int GetCollaborationsReceived(int actorNumber)
    {
        int count = 0;
        string prefix = actorNumber + "_";
        foreach (var kvp in collaborations)
            if (kvp.Key.StartsWith(prefix)) count += kvp.Value.Count;
        return count;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private PlayedCardsSpawner FindSpawnerByOwner(int cardOwnerActorNumber)
    {
        foreach (var spawner in FindObjectsOfType<PlayedCardsSpawner>())
        {
            if (spawner.actorNumber == cardOwnerActorNumber)
                return spawner;
        }
        return null;
    }

    private CollaborateButton FindCollaborateButton(int cardOwnerActorNumber, int cardIndex)
    {
        PlayedCardsSpawner spawner = FindSpawnerByOwner(cardOwnerActorNumber);
        if (spawner == null || cardIndex < 0 || cardIndex >= spawner.spawnedCards.Count) return null;
        return spawner.spawnedCards[cardIndex]?.GetComponent<CollaborateButton>();
    }

    private Player FindPlayer(int actorNumber)
    {
        foreach (var player in PhotonNetwork.PlayerList)
            if (player.ActorNumber == actorNumber) return player;
        return null;
    }

    private string GetPlayerName(int actorNumber)
    {
        Player player = FindPlayer(actorNumber);
        if (player != null && player.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var n))
            return n as string;
        return $"Player {actorNumber}";
    }

    private string GetPlayerCompany(int actorNumber)
    {
        Player player = FindPlayer(actorNumber);
        if (player != null && player.CustomProperties.TryGetValue(BiovalueStatics.PlayerCompanyKey, out var c))
            return c as string;
        return "";
    }

    private string GetCardName(int cardOwnerActorNumber, int cardIndex)
    {
        int ownerIndex = cardOwnerActorNumber - 1;
        if (!GameState.Instance.playerActionCards.TryGetValue(ownerIndex, out var cardIds)) return "";
        if (cardIndex < 0 || cardIndex >= cardIds.Count) return "";
        ActionCard card = ActionCardsDatabaseSession.Instance.SessionDb.GetActionCardById(cardIds[cardIndex]);
        return card?.cardName ?? "";
    }
}
