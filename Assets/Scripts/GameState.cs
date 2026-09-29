using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// Facade over the game's networked state. Turn order, game phase, and
// action-card sync are each owned by a focused, independent class
// (TurnAuthority / GamePhaseAuthority / ActionCardSyncService) — this class
// composes them and stays the single public entry point (Instance, RPCs,
// UnityEvents) so every existing caller and Inspector-wired button keeps
// working unchanged.
public class GameState : MonoBehaviourPunCallbacks
{
    #region Singleton

    private static GameState _instance;

    [SerializeField] private ActionCardsDatabase actionCardsDatabase;

    private readonly TurnAuthority _turnAuthority = new TurnAuthority();
    private readonly GamePhaseAuthority _phaseAuthority = new GamePhaseAuthority();
    private readonly ActionCardSyncService _actionCards = new ActionCardSyncService();

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

        // Restore state from room properties (works for both fresh joins and
        // rejoiners). Done in Awake so UI components that read the value in
        // their own Awake get a valid result.
        _turnAuthority.RestoreFromRoomProperties();
        _phaseAuthority.RestoreFromRoomProperties();
        _actionCards.RestoreFromRoomProperties();
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
        if (_turnAuthority.CurrentActorNumber > 0)
            onPlayerTurnIndexChanged.Invoke(_turnAuthority.CurrentActorNumber);
        if (_phaseAuthority.Current != GamePhase.None)
            onGamePhaseChanged.Invoke(_phaseAuthority.Current);
        foreach (var kvp in _actionCards.PlayerActionCards)
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
        // Phase changes are GM-only. Also enforced receiver-side in RPC_SetGamePhase
        // in case a modified client calls the RPC directly.
        if (!GameAuthority.IsGameMaster) return;

        // Persist in room so rejoiners can read the current phase.
        var roomProps = new Hashtable { { BiovalueStatics.GamePhaseKey, (int)gamePhase } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

        photonView.RPC(nameof(RPC_SetGamePhase), RpcTarget.All, gamePhase);
    }

    public GamePhase GetCurrentGamePhase() => _phaseAuthority.Current;

    [PunRPC]
    private void RPC_SetGamePhase(GamePhase gamePhase, PhotonMessageInfo info)
    {
        if (!GameAuthority.SenderIsGameMaster(info)) return;
        if (!_phaseAuthority.TrySet(gamePhase)) return;
        onGamePhaseChanged.Invoke(_phaseAuthority.Current);
    }

    public void DebugGamePhaseChanged(GamePhase gamePhase) =>
        Debug.LogWarning($"New Game Phase: {gamePhase}");

    #endregion

    #region BiovaluePlayerTurnSync

    [HideInInspector] public List<BiovaluePlayer> Players = new List<BiovaluePlayer>();

    // Public surface kept as int for event compatibility — now carries actor number, not position.
    public int _playerTurnIndex => _turnAuthority.CurrentActorNumber;

    [System.Serializable]
    public class OnTurnIndexChanged : UnityEvent<int> { }

    public OnTurnIndexChanged onPlayerTurnIndexChanged = new OnTurnIndexChanged();

    // GM-only: advances the turn regardless of whose turn it currently is.
    public void GMAdvanceTurn()
    {
        if (!GameAuthority.IsGameMaster) return;
        AdvanceTurnFrom(_turnAuthority.CurrentActorNumber);
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
        // Turn advancement is GM-only. Also enforced receiver-side in
        // RPC_SetTurnForActorNumber in case a modified client calls the RPC directly.
        if (!GameAuthority.IsGameMaster) return;

        // Persist so rejoiners immediately know whose turn it is.
        var roomProps = new Hashtable { { BiovalueStatics.TurnActorKey, actorNumber } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

        photonView.RPC(nameof(RPC_SetTurnForActorNumber), RpcTarget.All, actorNumber);
    }

    [PunRPC]
    private void RPC_SetTurnForActorNumber(int actorNumber, PhotonMessageInfo info)
    {
        if (!GameAuthority.SenderIsGameMaster(info)) return;

        _turnAuthority.SetCurrentActor(actorNumber);
        onPlayerTurnIndexChanged.Invoke(actorNumber);
        GameLog.Instance?.AddEntryLocal($"It's {GameLog.GetPlayerName(actorNumber)}'s turn");
    }

    public int GetCurrentPlayerTurnIndex() => _turnAuthority.CurrentActorNumber;

    public Player GetCurrentPlayer()
    {
        return PhotonNetwork.PlayerList.FirstOrDefault(p => p.ActorNumber == _turnAuthority.CurrentActorNumber);
    }

    public bool IsMyTurn() =>
        _turnAuthority.IsCurrentActor(PhotonNetwork.LocalPlayer.ActorNumber);

    // Advance to the next connected non-GM player after fromActorNumber,
    // skipping any actors deferred to the end of this cycle (late rejoiners).
    private void AdvanceTurnFrom(int fromActorNumber)
    {
        int next = _turnAuthority.ResolveNextActor(fromActorNumber);
        if (next > 0) SetTurnForActorNumber(next);
    }

    // Called by master when the current-turn player disconnects.
    public override void OnPlayerLeftRoom(Player other)
    {
        if (!GameAuthority.IsGameMaster) return;

        if (_turnAuthority.IsCurrentActor(other.ActorNumber))
        {
            Debug.LogWarning($"[GameState] Current-turn player ({other.ActorNumber}) disconnected — advancing turn.");
            AdvanceTurnFrom(other.ActorNumber);
        }
    }

    // Called on all clients when a player joins/rejoins mid-game.
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!GameAuthority.IsGameMaster) return;
        if (_phaseAuthority.Current == GamePhase.None) return; // game not started yet

        // Defer the rejoining player's turn until the current cycle completes,
        // so they don't jump the queue ahead of players who were already waiting.
        _turnAuthority.DeferToEndOfCycle(newPlayer.ActorNumber);

        // Push current state directly to the rejoiner so their UI matches everyone else.
        // Action cards are already in room properties and restored in their Awake.
        photonView.RPC(nameof(RPC_SyncStateToRejoiner), newPlayer,
            _turnAuthority.CurrentActorNumber, (int)_phaseAuthority.Current);

        // Send the full game log so the rejoiner sees what happened while they were gone.
        GameLog.Instance?.PushHistoryTo(newPlayer);

        Debug.Log($"[GameState] Rejoiner {newPlayer.ActorNumber} synced and deferred to end of turn cycle.");
    }

    [PunRPC]
    private void RPC_SyncStateToRejoiner(int turnActorNumber, int gamePhase)
    {
        _turnAuthority.SetCurrentActor(turnActorNumber);
        _phaseAuthority.TrySet((GamePhase)gamePhase);
        onPlayerTurnIndexChanged.Invoke(turnActorNumber);
        onGamePhaseChanged.Invoke(_phaseAuthority.Current);
        // Action cards were restored from room properties in Awake — just notify listeners.
        foreach (var kvp in _actionCards.PlayerActionCards)
            onPlayerActionCardsSet.Invoke(kvp.Key);
    }

    public void DebugPlayerTurnIndexChanged(int actorNumber) =>
        Debug.LogWarning($"New Turn Actor: {actorNumber}");

    #endregion

    #region ActionCardSync

    // Exposes the same dictionary reference callers already read directly
    // (GameState.Instance.playerActionCards.TryGetValue(...) etc.).
    public Dictionary<int, List<int>> playerActionCards => _actionCards.PlayerActionCards;

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
        _actionCards.SetCards(playerId, actionCardIds);
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
        _actionCards.SetDescription(playerId, cardId, description);
        onActionCardDescriptionChanged.Invoke(cardId);
    }

    [PunRPC]
    private void RPC_SetActionCardDescriptionHow(int playerId, int cardId, string descriptionHow)
    {
        _actionCards.SetDescriptionHow(playerId, cardId, descriptionHow);
        onActionCardDescriptionChanged.Invoke(cardId);
    }

    public string GetActionCardDescription(int playerId, int cardId) =>
        _actionCards.GetDescription(playerId, cardId);

    public string GetActionCardDescriptionHow(int playerId, int cardId) =>
        _actionCards.GetDescriptionHow(playerId, cardId);

    #endregion
}
