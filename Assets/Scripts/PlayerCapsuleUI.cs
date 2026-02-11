using TMPro;
using UnityEngine;

public class PlayerCapsuleUI : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNumberText;
    [SerializeField] private TMP_Text playerNameText;

    public void SetPlayer(int playerIndex1Based, string playerName)
    {
        if (playerNumberText) playerNumberText.text = $"Player {playerIndex1Based}";
        if (playerNameText)   playerNameText.text   = string.IsNullOrEmpty(playerName) ? "—" : playerName;
    }
    
}