using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Random = UnityEngine.Random;

public class LobbyNetworkController : MonoBehaviourPunCallbacks
{
    [SerializeField] private byte maxPlayers = 4;
    private const string ROLE_KEY = "role"; // "GM" | "Player"

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
        if (!PhotonNetwork.IsConnectedAndReady) { Debug.Log("Ainda a ligar ao Photon..."); return; }

        CurrentRoomCode = GenerateRoomCode(6);
        var opts = new RoomOptions { MaxPlayers = maxPlayers, IsOpen = true, IsVisible = false };
        PhotonNetwork.CreateRoom(CurrentRoomCode, opts, TypedLobby.Default);
    }

    public void JoinLobbyByCode(string codeRaw)
    {
        if (!PhotonNetwork.IsConnectedAndReady) { Debug.Log("Ainda a ligar ao Photon..."); return; }

        string code = (codeRaw ?? "").Trim().ToUpperInvariant();
        if (code.Length == 0) { Debug.LogWarning("Código inválido."); return; }

        CurrentRoomCode = code;
        PhotonNetwork.JoinRoom(code);
    }

    // ===== Callbacks essenciais =====
    public override void OnConnectedToMaster()
    {
        Debug.Log("[Photon] Connected.");
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"[Photon] Sala criada: {PhotonNetwork.CurrentRoom.Name}");
        // O papel é atribuído em OnJoinedRoom para garantir LocalPlayer disponível.
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
}

    
    

