using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Photon.Pun;

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
        if (createPanel == null) return;

        if (set)
        {
            LeanTween.cancel(createPanel);
            createPanel.SetActive(true);
            createPanel.GetComponent<RectTransform>().localScale = Vector3.zero;
            LeanTween.scale(createPanel.GetComponent<RectTransform>(), Vector3.one, 0.25f)
                .setEaseOutBack()
                .setIgnoreTimeScale(true);
        }
        else
        {
            LeanTween.cancel(createPanel);
            createPanel.GetComponent<RectTransform>().localScale = Vector3.one;
            LeanTween.scale(createPanel.GetComponent<RectTransform>(), Vector3.zero, 0.25f)
                .setEaseInBack()
                .setIgnoreTimeScale(true)
                .setOnComplete(() =>
                {
                    createPanel.SetActive(false);
                    createPanel.GetComponent<RectTransform>().localScale = Vector3.one;
                });
        }
    }
    
    public void OnClickBackToMainMenu()
    {
        StartCoroutine(GoBack());
    }

    private IEnumerator GoBack()
    {
        // Disable scene sync FIRST so Photon can't redirect the next scene load.
        PhotonNetwork.AutomaticallySyncScene = false;

        // Leave the room gracefully before disconnecting.
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            float leaveTimeout = 3f;
            while (PhotonNetwork.InRoom && leaveTimeout > 0f)
            {
                leaveTimeout -= Time.deltaTime;
                yield return null;
            }
        }

        if (PhotonNetwork.IsConnected)
            PhotonNetwork.Disconnect();

        // Wait for the FULLY disconnected state.
        // IsConnected can return false during Disconnecting, which is not safe yet.
        float timeout = 5f;
        while (PhotonNetwork.NetworkClientState != Photon.Realtime.ClientState.Disconnected && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        SceneManager.LoadScene(0);
    }
    
    public void SelectInstrumentPanel()
    {
        if (toSelectInstrumentPanel == null) return;

        LeanTween.cancel(toSelectInstrumentPanel);

        toSelectInstrumentPanel.SetActive(true);
        toSelectInstrumentPanel.transform.localScale = Vector3.zero;

        LeanTween.scale(toSelectInstrumentPanel, Vector3.one, 0.25f)
            .setEaseOutBack()
            .setIgnoreTimeScale(true);
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
    
    public void GoToNextPhase()
    {
        PhotonNetwork.LoadLevel("Role");
    }
}
