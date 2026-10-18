using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PhotonConnection : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject canva;

    // The Loading scene's screen (null in other scenes): shows the steps and opens the lobby itself.
    private static LoadingScreen Loader => LoadingScreen.Instance;

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        StartCoroutine(ConnectWhenReady());
    }

    // "Try again" on the loading screen.
    public void Retry()
    {
        StopAllCoroutines();
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
        {
            if (!PhotonNetwork.ConnectUsingSettings() && Loader) Loader.ShowError();
        }
        else if (PhotonNetwork.InLobby)
        {
            if (Loader) Loader.SetStep(2);
            else StartCoroutine(LoadNextScene());
        }
        else
        {
            if (Loader) Loader.SetStep(1);
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[Photon] Connected to Master.");
        if (Loader) Loader.SetStep(1);
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[Photon] Joined Lobby.");
        if (Loader)
        {
            Loader.SetStep(2); // the screen shows "Ready!" and opens the lobby
            return;
        }
        canva.GetComponent<FadingScript>().FadeIn();
        StartCoroutine(LoadNextScene());
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (cause == DisconnectCause.DisconnectByClientLogic) return; // we disconnected on purpose
        Debug.LogWarning("[Photon] Disconnected: " + cause);
        if (Loader) Loader.ShowError();
    }

    IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(2); // Lobby
    }
}
