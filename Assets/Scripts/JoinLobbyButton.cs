using TMPro;
using UnityEngine;

public class JoinLobbyButton : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeInput;   // arrasta o input
    [SerializeField] private LobbyNetworkController lobby;   // arrasta o controller

    public void OnClickJoin()
    {
        string raw = joinCodeInput != null ? joinCodeInput.text : null;
        Debug.Log($"[UI] Join raw='{raw}' len={(raw == null ? 0 : raw.Length)}");
        lobby.JoinLobbyByCode(raw);
        
    }
}
