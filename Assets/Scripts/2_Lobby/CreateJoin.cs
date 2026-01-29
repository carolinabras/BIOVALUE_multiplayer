using System.Text.RegularExpressions;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;




public class CreateJoin : MonoBehaviourPunCallbacks
{
    public TMP_InputField joinInputField;
    public TMP_InputField createInputField;
    public const string ROLE_KEY = "role"; // "GM" | "Player" 

    public static CreateJoin Instance { get; private set; }

    [SerializeField] private GameObject waitingPanel;
    [SerializeField] private GameObject lobbyConfig;
    
    public string currentRoomCode { get; private set; } = "";


    private void Awake()
    {
        // DontDestroyOnLoad(gameObject);
    }

    public void CreateRoom()
    {
        if (createInputField.text != "")
        {
            PhotonNetwork.CreateRoom(createInputField.text);
            lobbyConfig.SetActive(true);
            Debug.Log("[Photon] Tentando criar a sala: " + createInputField.text);
           
        }
        else
        {
            Debug.LogWarning("[Photon] Nome da sala inválido.");
        }

        Debug.Log("INPUT TEXT: " + createInputField.text);
    }

    public void JoinRoom()
    {
        if (joinInputField.text != "" )
        {
            PhotonNetwork.JoinRoom(joinInputField.text);
            Debug.Log("[Photon] Tentando entrar na sala: " + joinInputField.text);
        }
        else
        {
            Debug.LogWarning("[Photon] Nome da sala inválido.");
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[Photon] Entrei na sala {PhotonNetwork.CurrentRoom.Name} " +
                  $"({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})");


        string role = (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 1)
            ? "GM"
            : "Player";
        
        if (role == "Player")
        {
            waitingPanel.SetActive(true);
        }
        
        
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { ROLE_KEY, role } });
        Debug.Log($"[Photon] Papel atribuído: {role}");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[Photon] CreateRoom falhou ({returnCode}): {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[Photon] JoinRoom falhou ({returnCode}): {message}");
    }
    
    public void LeaveRoomToLoad()
    {
        
        PhotonNetwork.Disconnect();
        StartCoroutine(Reconect());
        
    }
    
    private System.Collections.IEnumerator Reconect()
    {
        yield return new WaitForSeconds(1);
        SceneManager.LoadScene(1);
    }
    
    
}