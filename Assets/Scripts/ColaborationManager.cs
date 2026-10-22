using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class ColaborationManager : MonoBehaviourPun
{
    public static ColaborationManager Instance;

    [SerializeField] private GameObject collabPopUpPrefab;
    [SerializeField] private Transform popUpParent;

    public Dictionary<string, List<int>> collaborations = new Dictionary<string, List<int>>();

    // Collaboration round (see CollaborationRound): tokens each player starts with, and the player
    // property that says they have finished collaborating.
    public const int DefaultStartTokens = 5;
    // Set by the GM in the Lobby (room property); the default when the game was started without the Lobby.
    public static int StartTokens =>
        PhotonNetwork.InRoom &&
        PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BiovalueStatics.StartTokensKey, out var v) && v is int n
            ? n : DefaultStartTokens;
    public const string CollabDoneKey = "collab_done";

    public enum RequestState { None, Pending, Collaborating, Declined }

    // This client's requests made from the collaboration round, and requests waiting on this client's cards.
    private readonly HashSet<string> _pendingOut = new HashSet<string>();
    private readonly HashSet<string> _declinedOut = new HashSet<string>();
    private readonly List<(int collaborator, int owner, int index)> _incoming = new List<(int, int, int)>();

    // Something about collaborations changed (requests, answers, collaborators).
    public event Action Changed;

    private void Awake()
    {
        Instance = this;
        RestoreFromRoom();
    }

    // Collaborations are also kept in the room (CollabKeyPrefix + "{owner actor}_{card index}" → the
    // collaborators' actors), so a player who rejoins gets them. The master client writes them.
    public const string CollabKeyPrefix = "co_";

    private void SaveToRoom(string key)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
        var list = collaborations.TryGetValue(key, out var l) ? l.ToArray() : new int[0];
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { CollabKeyPrefix + key, list } });
    }

    private void RestoreFromRoom()
    {
        if (!PhotonNetwork.InRoom) return;
        foreach (var entry in PhotonNetwork.CurrentRoom.CustomProperties)
            if (entry.Key is string key && key.StartsWith(CollabKeyPrefix) && entry.Value is int[] actors)
                collaborations[key.Substring(CollabKeyPrefix.Length)] = new List<int>(actors);
    }

    private static string Key(int cardOwnerActorNumber, int cardIndex) => $"{cardOwnerActorNumber}_{cardIndex}";

    public static int MyTokens() =>
        PhotonNetwork.LocalPlayer != null &&
        PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(BiovalueStatics.CollabTokensKey, out var t) && t is int n ? n : 0;

    public static bool IsDone(Player player) =>
        player != null && player.CustomProperties.TryGetValue(CollabDoneKey, out var d) && d is bool b && b;

    public static void SetMyDone(bool done)
    {
        var props = new Hashtable { [CollabDoneKey] = done };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    // Spends a token and asks the card's owner. Returns false when there is nothing to spend or already asked.
    public bool RequestFromRound(int cardOwnerActorNumber, int cardIndex)
    {
        int me = PhotonNetwork.LocalPlayer.ActorNumber;
        string key = Key(cardOwnerActorNumber, cardIndex);
        if (cardOwnerActorNumber == me || _pendingOut.Contains(key)) return false;
        if (GetCollaborators(cardOwnerActorNumber, cardIndex).Contains(me)) return false;
        int tokens = MyTokens();
        if (tokens <= 0) return false;

        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { [BiovalueStatics.CollabTokensKey] = tokens - 1 });
        _pendingOut.Add(key);
        _declinedOut.Remove(key);
        RequestCollaboration(me, cardOwnerActorNumber, cardIndex);
        Changed?.Invoke();
        return true;
    }

    // This client's view of a card: has it asked, is it in, was it declined.
    public RequestState MyState(int cardOwnerActorNumber, int cardIndex)
    {
        string key = Key(cardOwnerActorNumber, cardIndex);
        if (GetCollaborators(cardOwnerActorNumber, cardIndex).Contains(PhotonNetwork.LocalPlayer.ActorNumber)) return RequestState.Collaborating;
        if (_pendingOut.Contains(key)) return RequestState.Pending;
        if (_declinedOut.Contains(key)) return RequestState.Declined;
        return RequestState.None;
    }

    // The collaboration round is over (results, or the GM changed phase). Runs on every client:
    // requests this client made that nobody answered give their token back, and request pop-ups still
    // open on this client's cards close, so a late "Accept" can't add a collaboration after the round.
    public void EndRound()
    {
        if (_pendingOut.Count > 0)
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(
                new Hashtable { [BiovalueStatics.CollabTokensKey] = MyTokens() + _pendingOut.Count });
            Debug.Log($"[ColaborationManager] Round over: {_pendingOut.Count} unanswered request(s), token(s) given back.");
            _pendingOut.Clear();
        }
        _incoming.Clear();
        foreach (var popUp in FindObjectsByType<CollabPopUpHook>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Destroy(popUp.gameObject);
        Changed?.Invoke();
    }

    // Requests waiting for an answer on one of this client's cards.
    public int IncomingRequests(int cardOwnerActorNumber, int cardIndex)
    {
        int count = 0;
        foreach (var r in _incoming)
            if (r.owner == cardOwnerActorNumber && r.index == cardIndex) count++;
        return count;
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
        _incoming.Add((collaboratorActorNumber, cardOwnerActorNumber, cardIndex));
        Changed?.Invoke();

        string requesterName = GetPlayerName(collaboratorActorNumber);
        string requesterEntity = GetPlayerCompany(collaboratorActorNumber);
        var (cardName, cardDescription) = GetCardTexts(cardOwnerActorNumber, cardIndex);

        Transform parent = popUpParent != null ? popUpParent : FindObjectOfType<Canvas>().rootCanvas.transform; // on top of everything
        GameObject popUp = Instantiate(collabPopUpPrefab, parent);
        popUp.GetComponent<CollabPopUpHook>()?.Setup(requesterName, requesterEntity, cardName, cardDescription, collaboratorActorNumber, cardOwnerActorNumber, cardIndex);
    }

    // ── Owner response ────────────────────────────────────────────────────────

    public void ConfirmCollaboration(int collaboratorActorNumber, int cardOwnerActorNumber, int cardIndex)
    {
        _incoming.Remove((collaboratorActorNumber, cardOwnerActorNumber, cardIndex));
        Changed?.Invoke();
        SendCollaboration(collaboratorActorNumber, cardOwnerActorNumber, cardIndex);

        string ownerName = GetPlayerName(cardOwnerActorNumber);
        string collabName = GetPlayerName(collaboratorActorNumber);
        string cardName = GetCardName(cardOwnerActorNumber, cardIndex);
        GameLog.Instance?.Log(GameLog.Kind.CollabAccepted, $"{GameLog.B(ownerName)} accepted {GameLog.B(collabName)}’s collaboration on “<noparse>{cardName}</noparse>”");

        Player collaborator = FindPlayer(collaboratorActorNumber);
        if (collaborator != null)
            photonView.RPC(nameof(RPC_CollabAccepted), collaborator, collaboratorActorNumber, cardOwnerActorNumber, cardIndex);
    }

    public void DenyCollaboration(int collaboratorActorNumber, int cardOwnerActorNumber, int cardIndex)
    {
        _incoming.Remove((collaboratorActorNumber, cardOwnerActorNumber, cardIndex));
        Changed?.Invoke();
        string ownerName = GetPlayerName(cardOwnerActorNumber);
        string collabName = GetPlayerName(collaboratorActorNumber);
        string cardName = GetCardName(cardOwnerActorNumber, cardIndex);
        GameLog.Instance?.Log(GameLog.Kind.CollabDeclined, $"{GameLog.B(ownerName)} declined {GameLog.B(collabName)}’s collaboration on “<noparse>{cardName}</noparse>”");

        Player collaborator = FindPlayer(collaboratorActorNumber);
        if (collaborator != null)
            photonView.RPC(nameof(RPC_CollabDeclined), collaborator, collaboratorActorNumber, cardOwnerActorNumber, cardIndex);
    }

    [PunRPC]
    private void RPC_CollabAccepted(int collaboratorActorNumber, int cardOwnerActorNumber, int cardIndex)
    {
        if (_pendingOut.Remove(Key(cardOwnerActorNumber, cardIndex))) Changed?.Invoke();
    }

    [PunRPC]
    private void RPC_CollabDeclined(int collaboratorActorNumber, int cardOwnerActorNumber, int cardIndex)
    {
        string key = Key(cardOwnerActorNumber, cardIndex);
        if (_pendingOut.Remove(key)) // give the token back here
        {
            _declinedOut.Add(key);
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { [BiovalueStatics.CollabTokensKey] = MyTokens() + 1 });
            Changed?.Invoke();
        }
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
        SaveToRoom(key);
        Changed?.Invoke();

        PlayedCardsSpawner spawner = FindSpawnerByOwner(cardOwnerActorNumber);
        if (spawner == null)
        {
            Debug.LogWarning($"RPC_ShowCollaboration: spawner not found for owner {cardOwnerActorNumber}");
            return;
        }

        if (cardIndex < 0 || cardIndex >= spawner.spawnedCards.Count)
        {
            Debug.LogWarning($"RPC_ShowCollaboration: cardIndex {cardIndex} is invalid");
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
        SaveToRoom(key);
        Changed?.Invoke();

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

    // The card's name and description (for an open action, what its player wrote).
    private (string name, string description) GetCardTexts(int cardOwnerActorNumber, int cardIndex)
    {
        // Played cards are kept by the player's number (sorted position, GM = 0).
        int ownerIndex = TurnRibbon.PlayerNumberOf(cardOwnerActorNumber);
        if (!GameState.Instance.playerActionCards.TryGetValue(ownerIndex, out var cardIds)) return ("", "");
        if (cardIndex < 0 || cardIndex >= cardIds.Count) return ("", "");
        ActionCard card = ActionCardsDatabaseSession.Instance.SessionDb.GetActionCardById(cardIds[cardIndex]);
        var (what, _) = CollaborationRound.CardTexts(cardOwnerActorNumber, cardIds[cardIndex], card);
        string name = card != null && !string.IsNullOrEmpty(card.cardName) ? card.cardName : "Action card";
        return (name, what);
    }
}
