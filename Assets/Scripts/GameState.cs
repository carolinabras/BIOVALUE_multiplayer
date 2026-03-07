using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GameState : MonoBehaviourPun
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
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
        
        actionCardsDatabase = ActionCardsDatabaseSession.Instance.SessionDb;
    }

    #endregion
    
    public int localPlayerIndex = -1;
    
    
    private void Start()
    {
        Player[] playerList = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
        
        for (int i = 0; i < playerList.Length; i++)
        {
            if (playerList[i].IsLocal)
            {
                localPlayerIndex = i; 
            }
            
        }

        var props = new ExitGames.Client.Photon.Hashtable();
        props[BiovalueStatics.CollabTokensKey] = 5;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        onPlayerTurnIndexChanged.AddListener(DebugPlayerTurnIndexChanged);
        onGamePhaseChanged.AddListener(DebugGamePhaseChanged);
        
        
        
        
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
    public class OnGamePhaseChanged : UnityEvent<GamePhase>
    {
    }

    public OnGamePhaseChanged onGamePhaseChanged = new OnGamePhaseChanged();

    public void SetGamePhaseByIndex(int index)
    {
        if (Enum.IsDefined(typeof(GamePhase), index))
        {
            GamePhase gamePhase = (GamePhase)index;
            SetGamePhase(gamePhase);
        }
        else
        {
            Debug.LogError($"Invalid GamePhase index: {index}");
        }
    }
    
    public void SetGamePhase(GamePhase gamePhase)
    {
        photonView.RPC(nameof(RPC_SetGamePhase), RpcTarget.All, gamePhase);
    }

    public GamePhase GetCurrentGamePhase()
    {
        return _currentGamePhase;
    }

    [PunRPC]
    private void RPC_SetGamePhase(GamePhase gamePhase)
    {
        if (_currentGamePhase == gamePhase) return;

        _currentGamePhase = gamePhase;
        
        onGamePhaseChanged.Invoke(_currentGamePhase);
    }
    
    public void DebugGamePhaseChanged(GamePhase gamePhase)
    {
        Debug.LogWarning($"New Game Phase: {gamePhase}");
    }
    
    #endregion


    #region BiovaluePlayerTurnSync

    [HideInInspector] public List<BiovaluePlayer> Players = new List<BiovaluePlayer>();

    public int _playerTurnIndex = 1; // Start with 1 since 0 is reserved for the GM and not displayed on the board

    [System.Serializable]
    public class OnTurnIndexChanged : UnityEvent<int>
    {
    }

    public OnTurnIndexChanged onPlayerTurnIndexChanged = new OnTurnIndexChanged();

    public void NextTurn()
    {
        if (!IsMyTurn())
        {
            Debug.LogWarning("Attempted to end turn when it's not the local player's turn.");
            return;
        }
        
        int nextIndex = localPlayerIndex + 1;
        SetTurnForPlayerIndex(nextIndex);
    }
    
    

    public void SetTurnForPlayerIndex(int index)
    {
        if (index < 0)
        {
            Debug.LogError($"Invalid player index: {index}");
            return;
        }
        
        if (index >= PhotonNetwork.PlayerList.Length)
        {
            index = index % PhotonNetwork.PlayerList.Length; //loop
            
        }

        if (index == 0)
        {
            index++; // Skip index 0, which is reserved for the GM and not displayed on the board
        }

        photonView.RPC(nameof(RPC_SetTurnForPlayerIndex), RpcTarget.All, index);
    }

    public int GetCurrentPlayerTurnIndex()
    {
        return _playerTurnIndex;
    }
    
    public Player GetCurrentPlayer(int turnIndex)
    {

        return PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ElementAt(turnIndex);
    }

    public bool IsMyTurn()
    {
        return _playerTurnIndex == localPlayerIndex;
    }

    [PunRPC]
    private void RPC_SetTurnForPlayerIndex(int index)
    {
        _playerTurnIndex = index;
        
        onPlayerTurnIndexChanged.Invoke(_playerTurnIndex);
    }
    
    public void DebugPlayerTurnIndexChanged(int playerIndex)
    {
        Debug.LogWarning($"New Player Turn Index: {playerIndex}");
    }
    
    #endregion
    
    
    #region ActionCardSync 
    
    public Dictionary<int, List<int>> playerActionCards = new Dictionary<int, List<int>>(); // player id to list of ActionCard IDs
    
    public void SetPlayerActionCards(int playerId, List<int> actionCardIds)
    {
        photonView.RPC(nameof(RPC_SetPlayerActionCards), RpcTarget.All, playerId, actionCardIds.ToArray());
    }

    public int [] GetPlayerActionCards(int playerId, List<int> actionCardIds)
    {
        return  playerActionCards[playerId].ToArray();
    }
   

    [PunRPC]
    private void RPC_SetPlayerActionCards(int playerId, int[] actionCardIds)
    {
        playerActionCards[playerId] = actionCardIds.ToList();
        Debug.LogWarning($"Player {playerId} played action cards with IDs: {string.Join(", ", actionCardIds)}");
    }

    
    
    #endregion
    /*public void NextTurn()
    {
        int nextIndex = (_playerTurnIndex + 1) % Players.Count;
        SetTurnForPlayerIndex(nextIndex);
        //set
    }
    
    public void OnClickEndTurn()
    {
       NextTurn();
    }
    
    public bool IsLocalPlayersTurn()
    {
        if (!PhotonNetwork.InRoom) return false;
        if (Players == null || Players.Count == 0) return false;

        int index = _playerTurnIndex;
        if (index < 0 || index >= Players.Count) return false;

        int currentActor = Players[index].ActorNumber;

        return currentActor == PhotonNetwork.LocalPlayer.ActorNumber;
    }*/
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


    
}