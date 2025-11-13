using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] TMP_InputField joinCodeInput;
    [SerializeField] LobbyNetworkController lobby;
    [SerializeField] GameObject createPanel;
    

    public void OnClickJoin()
    {
        string raw = joinCodeInput != null ? joinCodeInput.text : null;
        Debug.Log($"[UI] Join raw='{raw}' len={(raw == null ? 0 : raw.Length)}");
        lobby.JoinLobbyByCode(raw);
        
    }

    public void OnCreateClicked()
    {
        lobby.CreateLobby();
        SetCreateLobbyPanel(true);
        
    }

    public void GoBackToLobby()
    {
        //lobby.LeaveLobby();
        SetCreateLobbyPanel(false);
    }
    

    private void SetCreateLobbyPanel(bool set)
    {
        if (createPanel != null)
        {
            createPanel.SetActive(set);
        }
    }
    
    public void OnClickBackToMainMenu()
    {
       
        StartCoroutine(GoBack());
    }
    
    private IEnumerator GoBack()
    {
        
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene(0);
    }
    
}
