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
    [SerializeField] private byte maxPlayers = 10;
    [SerializeField] private TextMeshProUGUI codeText;
    [SerializeField] private Button createButton;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject createPanel;
    
    private const string ROLE_KEY = "role"; // "GM" | "Player"
    
    
    
    private bool _createAfterLeaving = false; // flag para criar sala após sair da atual

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
        // 1) Só cria no Master
        if (PhotonNetwork.NetworkClientState != ClientState.ConnectedToMasterServer)
        {
            Debug.LogWarning($"CreateLobby: ainda não estou no Master (state={PhotonNetwork.NetworkClientState}).");
            return;
        }

        // 2) Garante que não estás numa sala
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("CreateLobby: estou numa sala; vou sair e criar a seguir.");
            _createAfterLeaving = true;
            PhotonNetwork.LeaveRoom(); // vai chamar OnLeftRoom
            return;
        }

        // 3) Criar
        CurrentRoomCode = GenerateRoomCode(6);
        var opts = new RoomOptions { MaxPlayers = (byte)maxPlayers, IsOpen = true, IsVisible = false };
        PhotonNetwork.CreateRoom(CurrentRoomCode, opts, TypedLobby.Default);
        codeText.text = CurrentRoomCode;
        createPanel.SetActive(true);
        
    }

    public void JoinLobbyByCode(string codeRaw)
    {
        // garante estado certo
        if (PhotonNetwork.NetworkClientState != ClientState.ConnectedToMasterServer)
        {
            Debug.LogWarning($"JoinLobbyByCode: ainda não no Master ({PhotonNetwork.NetworkClientState}).");
            return;
        }
        if (PhotonNetwork.InRoom)
        {
            Debug.LogWarning("JoinLobbyByCode: já estás numa sala.");
            return;
        }

        string code = NormalizeCode(codeRaw);
        if (code == null)
        {
            Debug.LogWarning($"Código inválido: '{codeRaw}'");
            return;
        }

        CurrentRoomCode = code;
        PhotonNetwork.JoinRoom(code);
        joinPanel.SetActive(true);
    }

    // ===== Callbacks essenciais =====
    public override void OnConnectedToMaster()
    {
        Debug.Log("[Photon] Connected.");
        // enable UI to create/join room
        createButton.interactable = true;
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"[Photon] Sala criada: {PhotonNetwork.CurrentRoom.Name}");
        // O papel é atribuído em OnJoinedRoom para garantir LocalPlayer disponível.
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
            // colisão de código → tenta outro automaticamente
            CreateLobby();
        }
    }
    
    public override void OnLeftRoom()
    {
        Debug.Log("[Photon] LeftRoom.");
        if (_createAfterLeaving)
        {
            _createAfterLeaving = false;
            // Chama de novo: agora já não estás na sala e (após OnConnectedToMaster) voltaste ao Master.
            CreateLobby();
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[Photon] Entrei na sala {PhotonNetwork.CurrentRoom.Name} " +
                  $"({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})");
        

        // Regra simples: quem criou (MasterClient e 1º na sala) é GM. Os restantes são Player.
        string role = (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 1)
                        ? "GM" : "Player";

        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { ROLE_KEY, role } });
        Debug.Log($"[Photon] Papel atribuído: {role}");
        
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
        // remove tudo o que não for A–Z ou 0–9 (tira “Código: ”, espaços, quebras de linha, etc.)
        s = Regex.Replace(s, @"[^A-Z0-9]", "");
        // os teus códigos têm 6 chars; ajusta se usares outro tamanho
        return s.Length >= 6 ? s : null;
    }
    
    
}

    
    

