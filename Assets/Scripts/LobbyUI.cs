using TMPro;
using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] TMP_InputField joinCodeInput;
    [SerializeField] LobbyNetworkController lobby;

    public void OnClickJoin()
    {
        string raw = joinCodeInput != null ? joinCodeInput.text : null;
        Debug.Log($"[UI] Join raw='{raw}' len={(raw == null ? 0 : raw.Length)}");
        lobby.JoinLobbyByCode(raw);
        
    }

    public void OnCreateClicked()
    {
        lobby.CreateLobby();
    }
}
