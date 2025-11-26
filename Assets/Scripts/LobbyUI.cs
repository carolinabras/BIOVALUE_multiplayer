using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] TMP_InputField joinCodeInput;
    [SerializeField] LobbyNetworkController lobby;
    [SerializeField] GameObject createPanel;
    [SerializeField] GameObject selectedInstrumentPanel;
    [SerializeField] GameObject selectedObjectivePanel;
    
    [SerializeField] GameObject toSelectInstrumentPanel;
    [SerializeField] GameObject toSelectObjectivePanel;
    [SerializeField] GameObject lobbyPanel;


    public void OnClickJoin()
    {
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
        }
       
    }

    /*public void OnClickJoin()
    {
        string raw = joinCodeInput != null ? joinCodeInput.text : null;
        Debug.Log($"[UI] Join raw='{raw}' len={(raw == null ? 0 : raw.Length)}");
        lobby.JoinLobbyByCode(raw);
        
    }*/

    public void OnCreateClicked()
    {
        //lobby.CreateLobby();
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
    
    public void SelectInstrumentPanel()
    {
        if (toSelectInstrumentPanel != null)
        {
            toSelectInstrumentPanel.SetActive(true);
        }
    }
    
    public void SelectObjectivePanel()
    {
        if (toSelectObjectivePanel != null)
        {
            toSelectObjectivePanel.SetActive(true);
        }
    }
    
    public void OnClickSelectedInstrument()
    {
        //if selected X number of instruments this becames available
        //maybe add a data manager to see how many instruments are selected and this part only does the UI change
        Debug.Log("[UI] Selected instrument clicked");
        //close panel 
        if (selectedInstrumentPanel != null)
        {
            selectedInstrumentPanel.SetActive(false);
        }
        
    }
    /*

    public void OnClickselectedObjective()
    {
        if (selectedObjectivePanel != null)
        {
            selectedObjectivePanel.SetActive(false);
        }
    }
     */
}
