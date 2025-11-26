using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PhotonConnection : MonoBehaviourPunCallbacks
{
    public static PhotonConnection Instance { get; private set; }
    public bool IsConnected => PhotonNetwork.IsConnectedAndReady;

    [SerializeField] private GameObject canva;

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
        if (PhotonNetwork.InLobby)
        {
            Debug.Log("[Photon] Already in Lobby.");
            
            SceneManager.LoadScene("Lobby"); //Lobby
        }
        PhotonNetwork.AutomaticallySyncScene = true; //important 
        if (!PhotonNetwork.IsConnected)
            PhotonNetwork.ConnectUsingSettings();

       
    }
    
    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true; //important 
        if (!PhotonNetwork.IsConnected)
            PhotonNetwork.ConnectUsingSettings();
        
    }
    
    
    
    public override void OnConnectedToMaster()
    {
        if (!PhotonNetwork.InLobby)
        {
            Debug.Log("[Photon] Connected to Master.");
            PhotonNetwork.JoinLobby();
        }
        
    }
    
    public override void OnJoinedLobby()
    {
        Debug.Log("[Photon] Joined Lobby.");
        canva.gameObject.GetComponent<FadingScript>().FadeIn();
        
        StartCoroutine(LoadNextScene());
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[Photon] Disconnected: {cause}");
    }
    
    IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene("Lobby"); //Lobby
    }
    
}
