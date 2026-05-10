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
    [SerializeField] private TMP_Text roomNameText;

    [SerializeField] private Button createButton;
    [SerializeField] private Button joinButton;

    private const float AlphaEmpty = 0.2f;
    private const float AlphaFull  = 1.0f;

    private CanvasGroup _createGroup;
    private CanvasGroup _joinGroup;

    public string currentRoomCode { get; private set; } = "";

    private void Awake()
    {
        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (createButton != null)
        {
            _createGroup = createButton.gameObject.GetComponent<CanvasGroup>();
            if (_createGroup == null) _createGroup = createButton.gameObject.AddComponent<CanvasGroup>();
            _createGroup.alpha = AlphaEmpty;
            createInputField.onValueChanged.AddListener(_ =>
                _createGroup.alpha = createInputField.text.Length > 0 ? AlphaFull : AlphaEmpty);
        }

        if (joinButton != null)
        {
            _joinGroup = joinButton.gameObject.GetComponent<CanvasGroup>();
            if (_joinGroup == null) _joinGroup = joinButton.gameObject.AddComponent<CanvasGroup>();
            _joinGroup.alpha = AlphaEmpty;
            joinInputField.onValueChanged.AddListener(_ =>
                _joinGroup.alpha = joinInputField.text.Length > 0 ? AlphaFull : AlphaEmpty);
        }
    }

    public void CreateRoom()
    {
        if (createInputField.text != "")
        {
            PhotonNetwork.CreateRoom(createInputField.text);
            if (roomNameText != null) roomNameText.text = createInputField.text;
            LeanTween.cancel(lobbyConfig);
            lobbyConfig.SetActive(true);
            lobbyConfig.GetComponent<RectTransform>().localScale = Vector3.zero;
            LeanTween.scale(lobbyConfig.GetComponent<RectTransform>(), Vector3.one, 0.25f)
                .setEaseOutBack()
                .setIgnoreTimeScale(true);
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