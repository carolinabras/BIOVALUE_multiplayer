using System;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class TurnoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _turnText;
    
    public Player currentPlayer;
    

    public void Awake()
    {
      GameState.Instance.onPlayerTurnIndexChanged.AddListener(OnTurnIndexChanged);
      OnTurnIndexChanged(GameState.Instance._playerTurnIndex);
    }

    public void OnTurnIndexChanged(int turnIndex)
    {
        
        currentPlayer = GameState.Instance.GetCurrentPlayer(turnIndex);
        if (currentPlayer.IsLocal)
        {
            _turnText.text = "Your Turn";
        }
        else
        {
            _turnText.text = currentPlayer.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var name) ? $"{name}'s Turn" : "Unknown Player's Turn";
        }
        
    }
}