using TMPro;
using UnityEngine;

public class PlayerCapsuleUI : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNumberText;
    [SerializeField] private TMP_Text playerNameText;

    // Chamas isto depois de instanciar
    public void Setup(int playerNumber, string playerName)
    {
        if (playerNumberText != null)
            playerNumberText.text = $"Player {playerNumber}";

        if (playerNameText != null)
            playerNameText.text = playerName;
    }
}