using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;

public class GameState : MonoBehaviourPun
{
    #region Singleton

    private static GameState _instance;

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
    }

    #endregion
    
    private void Start()
    {
        Player[] playerList = PhotonNetwork.PlayerList;
        foreach (var player in playerList)
        {
            Players.Add(new BiovaluePlayer(player));
        }
        
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

    private int _playerTurnIndex = 0;

    [System.Serializable]
    public class OnTurnIndexChanged : UnityEvent<int>
    {
    }

    public OnTurnIndexChanged onPlayerTurnIndexChanged = new OnTurnIndexChanged();

    public void SetTurnForPlayerIndex(int index)
    {
        if (index < 0 || index >= Players.Count)
        {
            Debug.LogError($"Invalid player index: {index}");
            return;
        }

        photonView.RPC(nameof(RPC_SetGamePhase), RpcTarget.All, index);
    }

    public int GetCurrentPlayerTurnIndex()
    {
        return _playerTurnIndex;
    }

    [PunRPC]
    private void RPC_SetTurnForPlayerIndex(int index)
    {
        if (index < 0 || index >= Players.Count) return;
        
        if (_playerTurnIndex == index) return;

        _playerTurnIndex = index;
        
        onPlayerTurnIndexChanged.Invoke(_playerTurnIndex);
    }
    
    public void DebugPlayerTurnIndexChanged(int playerIndex)
    {
        Debug.LogWarning($"New Player Turn Index: {playerIndex}");
    }
    
    #endregion

}