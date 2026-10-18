using System.Text.RegularExpressions;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class LobbyNetworkController : MonoBehaviourPunCallbacks
{
    [SerializeField] private byte maxPlayers = 8;
    [SerializeField] private TextMeshProUGUI codeText;
    [SerializeField] private Button createButton;
    [SerializeField] private GameObject joinPanel;
   // [SerializeField] private GameObject createPanel;
    
    private const string ROLE_KEY = "role"; // "GM" | "Player"
    
    
    
    private bool _createAfterLeaving = false; // flag to create a room after leaving the current one

    public string CurrentRoomCode { get; private set; } = "";

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        if (!PhotonNetwork.IsConnected)
            PhotonNetwork.ConnectUsingSettings();
    }

    // ===== API =====
    public void CreateLobby()
    {
        // create on the master server
        if (PhotonNetwork.NetworkClientState != ClientState.ConnectedToMasterServer)
        {
            Debug.LogWarning($"CreateLobby: not on the Master server yet (state={PhotonNetwork.NetworkClientState}).");
            return;
        }

        // roomcheck
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("CreateLobby: already in a room; leaving it and creating afterwards.");
            _createAfterLeaving = true;
            PhotonNetwork.LeaveRoom(); // will call OnLeftRoom
            return;
        }

        // create room 
        CurrentRoomCode = GenerateRoomCode(6);
        // PlayerTtl: Photon holds the player slot for 60 s after a disconnect so they can rejoin.
        // EmptyRoomTtl: keeps the room alive for 60 s even if all players temporarily drop.
        var opts = new RoomOptions
        {
            MaxPlayers = (byte)maxPlayers,
            IsOpen = true,
            IsVisible = false,
            PlayerTtl = 60000,
            EmptyRoomTtl = 60000,
        };
        PhotonNetwork.CreateRoom(CurrentRoomCode, opts, TypedLobby.Default);
        codeText.text = CurrentRoomCode;
        //createPanel.SetActive(true);
        
    }

    public void JoinLobbyByCode(string codeRaw)
    {
        // make sure we are in the right state
        if (PhotonNetwork.NetworkClientState != ClientState.ConnectedToMasterServer)
        {
            Debug.LogWarning($"JoinLobbyByCode: not on the Master server yet ({PhotonNetwork.NetworkClientState}).");
            return;
        }
        if (PhotonNetwork.InRoom)
        {
            Debug.LogWarning("JoinLobbyByCode: already in a room.");
            return;
        }

        string code = NormalizeCode(codeRaw);
        if (code == null)
        {
            Debug.LogWarning($"Invalid code: '{codeRaw}'");
            return;
        }

        CurrentRoomCode = code;
        PhotonNetwork.JoinRoom(code);
        joinPanel.SetActive(true);
    }

    // ===== Callbacks =====
    public override void OnConnectedToMaster()
    {
        Debug.Log("[Photon] Connected.");
        // enable UI to create/join room
        createButton.interactable = true;
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"[Photon] Room created: {PhotonNetwork.CurrentRoom.Name}");
        
    }

    public void GoToNextPhase()
    {
        PhotonNetwork.LoadLevel("Role");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[Photon] CreateRoom falhou ({returnCode}): {message}");
        if (returnCode == ErrorCode.GameIdAlreadyExists)
        {
           
            CreateLobby();
        }
    }
    
    public override void OnLeftRoom()
    {
        Debug.Log("[Photon] LeftRoom.");
        if (_createAfterLeaving)
        {
            _createAfterLeaving = false;
            
            CreateLobby();
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[Photon] Joined room {PhotonNetwork.CurrentRoom.Name} " +
                  $"({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})");
        

        
        string role = (GameAuthority.IsGameMaster && PhotonNetwork.CurrentRoom.PlayerCount == 1)
                        ? "GM" : "Player";

        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { ROLE_KEY, role } });
        Debug.Log($"[Photon] Role assigned: {role}");
        
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[Photon] JoinRoom falhou ({returnCode}): {message}");
        CurrentRoomCode = "";
    }

    // ===== Helpers =====
    public static bool IAmGM()
    {
        return PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(ROLE_KEY, out var v)
               && (string)v == "GM";
    }

    private static string GenerateRoomCode(int len)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        char[] buf = new char[len];
        for (int i = 0; i < len; i++) buf[i] = alphabet[Random.Range(0, alphabet.Length)];
        return new string(buf);
    }
    
    private static string NormalizeCode(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim().ToUpperInvariant();
        
        s = Regex.Replace(s, @"[^A-Z0-9]", "");
        
        return s.Length >= 6 ? s : null;
    }
    
    
}

    
    

