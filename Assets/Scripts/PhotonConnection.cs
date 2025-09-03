using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonConnection : MonoBehaviourPunCallbacks
{
    public static PhotonConnection Instance { get; private set; }
    public bool IsConnected => PhotonNetwork.IsConnectedAndReady;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        if (!PhotonNetwork.IsConnected)
            PhotonNetwork.ConnectUsingSettings();
    }
    
    public override void OnConnectedToMaster()
    {
        Debug.Log("[Photon] Connected to Master.");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[Photon] Disconnected: {cause}");
    }
    
}
