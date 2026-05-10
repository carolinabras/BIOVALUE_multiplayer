using System.Linq;
using Photon.Pun;
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

    // actorNumber is now the actor number of the player whose turn it is.
    public void OnTurnIndexChanged(int actorNumber)
    {
        currentPlayer = PhotonNetwork.PlayerList.FirstOrDefault(p => p.ActorNumber == actorNumber);
        if (currentPlayer == null) return;

        _turnText.text = currentPlayer.IsLocal
            ? "Your Turn"
            : currentPlayer.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var name)
                ? $"{name}'s Turn"
                : "Unknown Player's Turn";
    }
}
