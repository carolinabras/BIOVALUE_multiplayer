using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PhotonConnection : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject canva;

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        StartCoroutine(ConnectWhenReady());
    }

    private IEnumerator ConnectWhenReady()
    {
        // Wait for any in-progress disconnect to finish fully.
        while (PhotonNetwork.NetworkClientState == ClientState.Disconnecting)
            yield return null;

        // One extra frame so the SDK finishes its internal cleanup —
        // ConnectUsingSettings can fail silently if called the same frame
        // the Disconnected state is reached.
        yield return null;

        if (!PhotonNetwork.IsConnected)
            PhotonNetwork.ConnectUsingSettings();
        else if (PhotonNetwork.InLobby)
            StartCoroutine(LoadNextScene());
        else
            PhotonNetwork.JoinLobby();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[Photon] Connected to Master.");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[Photon] Joined Lobby.");
        canva.GetComponent<FadingScript>().FadeIn();
        StartCoroutine(LoadNextScene());
    }

    IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(2); // Lobby
    }
}
