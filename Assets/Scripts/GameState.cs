using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GameState : MonoBehaviourPunCallbacks
{
    #region Singleton

    private static GameState _instance;

    [SerializeField] private ActionCardsDatabase actionCardsDatabase;

    public static GameState Instance
    {
        get
        {
            if (_instance) return _instance;
            _instance = FindFirstObjectByType<GameState>();
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
            _instance = this;
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        actionCardsDatabase = ActionCardsDatabaseSession.Instance.SessionDb;

        // Restore turn actor from room properties (works for both fresh joins and rejoiners).
        // Done in Awake so UI components that read the value in their own Awake get a valid result.
        if (PhotonNetwork.InRoom)
        {
            var props = PhotonNetwork.CurrentRoom.CustomProperties;

            if (props.TryGetValue(BiovalueStatics.TurnActorKey, out var turnActor))
                _playerTurnActorNumber = (int)turnActor;
            else
            {
                // Fresh game — default to the first non-GM player (sorted index 1).
                var sorted = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
                _playerTurnActorNumber = sorted.Length > 1 ? sorted[1].ActorNumber : -1;
            }

            if (props.TryGetValue(BiovalueStatics.GamePhaseKey, out var phase))
                _currentGamePhase = (GamePhase)(int)phase;

            // Restore action cards for every player slot.
            for (int i = 0; i < 8; i++)
            {
                string key = BiovalueStatics.ActionCardsKeyPrefix + i;
                if (props.TryGetValue(key, out var cards) && cards is int[] arr)
                    playerActionCards[i] = arr.ToList();
            }
        }
    }

    #endregion

    // localPlayerIndex is now dynamic — position of local player in actor-sorted list.
    public int localPlayerIndex
    {
        get
        {
            var sorted = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
            for (int i = 0; i < sorted.Length; i++)
                if (sorted[i].IsLocal) return i;
            return -1;
        }
    }

    private void Start()
    {
        // Only initialise tokens on a genuine first join — not on a rejoin where
        // the player's custom properties (and remaining tokens) are still held by Photon.
        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(BiovalueStatics.CollabTokensKey))
        {
            var props = new Hashtable();
            props[BiovalueStatics.CollabTokensKey] = 5;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        onPlayerTurnIndexChanged.AddListener(DebugPlayerTurnIndexChanged);
        onGamePhaseChanged.AddListener(DebugGamePhaseChanged);

        // Fire events with the values already initialised in Awake (from room properties).
        // This is the moment all other components have registered their listeners, so the
        // events reach everyone — critical for rejoiners who need their UI refreshed.
        if (_playerTurnActorNumber > 0)
            onPlayerTurnIndexChanged.Invoke(_playerTurnActorNumber);
        if (_currentGamePhase != GamePhase.None)
            onGamePhaseChanged.Invoke(_currentGamePhase);
        foreach (var kvp in playerActionCards)
            onPlayerActionCardsSet.Invoke(kvp.Key);
    }

    #region GamePhaseSync

    [Serializable]
    public enum GamePhase
    {
        None = 0,
        InstrumentSelection = 1,
        ActionCardPlay = 2,
        Collaboration = 3,
    }

    private GamePhase _currentGamePhase = GamePhase.None;

    [Serializable]
    public class OnGamePhaseChanged : UnityEvent<GamePhase> { }

    public OnGamePhaseChanged onGamePhaseChanged = new OnGamePhaseChanged();

    public void SetGamePhaseByIndex(int index)
    {
        if (Enum.IsDefined(typeof(GamePhase), index))
            SetGamePhase((GamePhase)index);
        else
            Debug.LogError($"Invalid GamePhase index: {index}");
    }

    public void SetGamePhase(GamePhase gamePhase)
    {
        // Persist in room so rejoiners can read the current phase.
        var roomProps = new Hashtable { { BiovalueStatics.GamePhaseKey, (int)gamePhase } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

        photonView.RPC(nameof(RPC_SetGamePhase), RpcTarget.All, gamePhase);
    }

    public GamePhase GetCurrentGamePhase() => _currentGamePhase;

    [PunRPC]
    private void RPC_SetGamePhase(GamePhase gamePhase)
    {
        if (_currentGamePhase == gamePhase) return;
        _currentGamePhase = gamePhase;
        onGamePhaseChanged.Invoke(_currentGamePhase);
    }

    public void DebugGamePhaseChanged(GamePhase gamePhase) =>
        Debug.LogWarning($"New Game Phase: {gamePhase}");

    #endregion

    #region BiovaluePlayerTurnSync

    [HideInInspector] public List<BiovaluePlayer> Players = new List<BiovaluePlayer>();

    // Internal: the actor number of whoever's turn it is (stable across disconnections).
    private int _playerTurnActorNumber = -1;

    // Public surface kept as int for event compatibility — now carries actor number, not position.
    public int _playerTurnIndex => _playerTurnActorNumber;

    [System.Serializable]
    public class OnTurnIndexChanged : UnityEvent<int> { }

    public OnTurnIndexChanged onPlayerTurnIndexChanged = new OnTurnIndexChanged();

    public void NextTurn()
    {
        if (!IsMyTurn())
        {
            Debug.LogWarning("Attempted to end turn when it's not the local player's turn.");
            return;
        }
        AdvanceTurnFrom(_playerTurnActorNumber);
    }

    // GM-only: advances the turn regardless of whose turn it currently is.
    public void GMAdvanceTurn()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        AdvanceTurnFrom(_playerTurnActorNumber);
    }

    // Keep old signature — converts position index to actor number.
    public void SetTurnForPlayerIndex(int index)
    {
        if (index < 0) return;
        var sorted = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
        if (sorted.Length == 0) return;

        index = index % sorted.Length;
        if (index == 0 && sorted.Length > 1) index = 1; // skip GM
        if (index >= sorted.Length) return;

        SetTurnForActorNumber(sorted[index].ActorNumber);
    }

    public void SetTurnForActorNumber(int actorNumber)
    {
        // Persist so rejoiners immediately know whose turn it is.
        var roomProps = new Hashtable { { BiovalueStatics.TurnActorKey, actorNumber } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

        photonView.RPC(nameof(RPC_SetTurnForActorNumber), RpcTarget.All, actorNumber);
    }

    [PunRPC]
    private void RPC_SetTurnForActorNumber(int actorNumber)
    {
        _playerTurnActorNumber = actorNumber;
        onPlayerTurnIndexChanged.Invoke(actorNumber);
        GameLog.Instance?.AddEntryLocal($"It's {GameLog.GetPlayerName(actorNumber)}'s turn");
    }

    public int GetCurrentPlayerTurnIndex() => _playerTurnActorNumber;

    public Player GetCurrentPlayer()
    {
        return PhotonNetwork.PlayerList.FirstOrDefault(p => p.ActorNumber == _playerTurnActorNumber);
    }

    public bool IsMyTurn() =>
        _playerTurnActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;

    // Advance to the next connected non-GM player after fromActorNumber,
    // skipping any actors deferred to the end of this cycle (late rejoiners).
    private void AdvanceTurnFrom(int fromActorNumber)
    {
        var sorted = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
        if (sorted.Length <= 1) return; // only GM left

        HashSet<int> skipSet = GetSkipActors();

        // Find next eligible player (higher actor number, not deferred)
        Player next = sorted.Skip(1)
            .FirstOrDefault(p => p.ActorNumber > fromActorNumber && !skipSet.Contains(p.ActorNumber));

        if (next == null)
        {
            // End of cycle — admit deferred players back into rotation.
            if (skipSet.Count > 0) ClearSkipActors();
            next = sorted.Skip(1).FirstOrDefault(); // wrap to first non-GM
        }

        if (next != null) SetTurnForActorNumber(next.ActorNumber);
    }

    private HashSet<int> GetSkipActors()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BiovalueStatics.SkipActorsKey, out var v) && v is int[] arr)
            return new HashSet<int>(arr);
        return new HashSet<int>();
    }

    private void ClearSkipActors()
    {
        PhotonNetwork.CurrentRoom.SetCustomProperties(
            new Hashtable { { BiovalueStatics.SkipActorsKey, new int[0] } });
    }

    // Called by master when the current-turn player disconnects.
    public override void OnPlayerLeftRoom(Player other)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (other.ActorNumber == _playerTurnActorNumber)
        {
            Debug.LogWarning($"[GameState] Current-turn player ({other.ActorNumber}) disconnected — advancing turn.");
            AdvanceTurnFrom(other.ActorNumber);
        }
    }

    // Called on all clients when a player joins/rejoins mid-game.
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (_currentGamePhase == GamePhase.None) return; // game not started yet

        // Defer the rejoining player's turn until the current cycle completes,
        // so they don't jump the queue ahead of players who were already waiting.
        var skip = GetSkipActors();
        skip.Add(newPlayer.ActorNumber);
        PhotonNetwork.CurrentRoom.SetCustomProperties(
            new Hashtable { { BiovalueStatics.SkipActorsKey, skip.ToArray() } });

        // Push current state directly to the rejoiner so their UI matches everyone else.
        // Action cards are already in room properties and restored in their Awake.
        photonView.RPC(nameof(RPC_SyncStateToRejoiner), newPlayer,
            _playerTurnActorNumber, (int)_currentGamePhase);

        // Send the full game log so the rejoiner sees what happened while they were gone.
        GameLog.Instance?.PushHistoryTo(newPlayer);

        Debug.Log($"[GameState] Rejoiner {newPlayer.ActorNumber} synced and deferred to end of turn cycle.");
    }

    [PunRPC]
    private void RPC_SyncStateToRejoiner(int turnActorNumber, int gamePhase)
    {
        _playerTurnActorNumber = turnActorNumber;
        _currentGamePhase = (GamePhase)gamePhase;
        onPlayerTurnIndexChanged.Invoke(turnActorNumber);
        onGamePhaseChanged.Invoke(_currentGamePhase);
        // Action cards were restored from room properties in Awake — just notify listeners.
        foreach (var kvp in playerActionCards)
            onPlayerActionCardsSet.Invoke(kvp.Key);
    }

    public void DebugPlayerTurnIndexChanged(int actorNumber) =>
        Debug.LogWarning($"New Turn Actor: {actorNumber}");

    #endregion

    #region ActionCardSync

    public Dictionary<int, List<int>> playerActionCards = new Dictionary<int, List<int>>();

    public void SetPlayerActionCards(int playerId, List<int> actionCardIds)
    {
        // Persist per-player card list so rejoiners can restore their state.
        var roomProps = new Hashtable { { BiovalueStatics.ActionCardsKeyPrefix + playerId, actionCardIds.ToArray() } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

        photonView.RPC(nameof(RPC_SetPlayerActionCards), RpcTarget.All, playerId, actionCardIds.ToArray());
    }

    public int[] GetPlayerActionCards(int playerId, List<int> actionCardIds)
    {
        return playerActionCards[playerId].ToArray();
    }

    [PunRPC]
    private void RPC_SetPlayerActionCards(int playerId, int[] actionCardIds)
    {
        playerActionCards[playerId] = actionCardIds.ToList();
        Debug.LogWarning($"Player {playerId} played action cards with IDs: {string.Join(", ", actionCardIds)}");
        onPlayerActionCardsSet.Invoke(playerId);

        string playerName = GameLog.GetPlayerNameByIndex(playerId);
        var cardNames = actionCardIds
            .Select(id => actionCardsDatabase.GetActionCardById(id)?.cardName ?? "Unknown Card");
        GameLog.Instance?.AddEntryLocal($"{playerName} played: {string.Join(", ", cardNames)}");
    }

    #endregion

    #region RejectedCount

    public Dictionary<int, int> playerRejectedCount = new Dictionary<int, int>();

    public void IncrementRejectedCount(int playerId)
    {
        photonView.RPC(nameof(RPC_IncrementRejectedCount), RpcTarget.All, playerId);
    }

    [PunRPC]
    private void RPC_IncrementRejectedCount(int playerId)
    {
        if (!playerRejectedCount.ContainsKey(playerId))
            playerRejectedCount[playerId] = 0;
        playerRejectedCount[playerId]++;
        Debug.Log($"Player {playerId} rejected count: {playerRejectedCount[playerId]}");
    }

    public int GetRejectedCount(int playerId)
    {
        Debug.LogWarning($"Player {playerId} rejected count");
        return playerRejectedCount.TryGetValue(playerId, out int count) ? count : 0;
    }

    #endregion

    #region ActionCardDescriptions

    public Dictionary<string, string> actionCardDescriptions = new Dictionary<string, string>();
    public Dictionary<string, string> actionCardDescriptionsHow = new Dictionary<string, string>();

    public UnityEvent<int> onActionCardDescriptionChanged = new UnityEvent<int>();
    public UnityEvent<int> onPlayerActionCardsSet = new UnityEvent<int>();

    public void SetActionCardDescription(int playerId, int cardId, string description)
    {
        photonView.RPC(nameof(RPC_SetActionCardDescription), RpcTarget.All, playerId, cardId, description);
    }

    public void SetActionCardDescriptionHow(int playerId, int cardId, string descriptionHow)
    {
        photonView.RPC(nameof(RPC_SetActionCardDescriptionHow), RpcTarget.All, playerId, cardId, descriptionHow);
    }

    [PunRPC]
    private void RPC_SetActionCardDescription(int playerId, int cardId, string description)
    {
        actionCardDescriptions[$"{playerId}_{cardId}"] = description;
        onActionCardDescriptionChanged.Invoke(cardId);
    }

    [PunRPC]
    private void RPC_SetActionCardDescriptionHow(int playerId, int cardId, string descriptionHow)
    {
        actionCardDescriptionsHow[$"{playerId}_{cardId}"] = descriptionHow;
        onActionCardDescriptionChanged.Invoke(cardId);
    }

    public string GetActionCardDescription(int playerId, int cardId) =>
        actionCardDescriptions.TryGetValue($"{playerId}_{cardId}", out string desc) ? desc : string.Empty;

    public string GetActionCardDescriptionHow(int playerId, int cardId) =>
        actionCardDescriptionsHow.TryGetValue($"{playerId}_{cardId}", out string desc) ? desc : string.Empty;

    #endregion
}
