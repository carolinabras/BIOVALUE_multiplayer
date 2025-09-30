using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class ChatManager : MonoBehaviourPun
{
    [Header("UI")]
    [SerializeField] private TMP_InputField inputField;        
    [SerializeField] private RectTransform messagesContent;    
    [SerializeField] private GameObject messagePrefab;         
    [SerializeField] private ScrollRect scrollRect;            

    private void Awake()
    {
        if (inputField != null) inputField.lineType = TMP_InputField.LineType.SingleLine;

      
        if (PlayerPrefs.HasKey("PlayerName"))
        {
            PhotonNetwork.NickName = PlayerPrefs.GetString("PlayerName");
        }
        else if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("player_name", out var n))
        {
            PhotonNetwork.NickName = n as string;
        }
        else
        {
            // fallback: ID do Photon
            PhotonNetwork.NickName = $"Player {PhotonNetwork.LocalPlayer?.ActorNumber}";
        }
    }

    private void Start()
    {
        // Enter envia
        if (inputField != null) inputField.onSubmit.AddListener(OnInputSubmit);
    }

    public void OnInputSubmit(string _)
    {
        OnSendClicked();
    }

    public void OnSendClicked()
    {
        if (inputField == null || messagesContent == null || messagePrefab == null)
        {
            Debug.LogError("[Chat] Falta ligar InputField / MessagesContent / MessagePrefab no Inspector.");
            return;
        }
        if (photonView == null)
        {
            Debug.LogError("[Chat] Falta o PhotonView no ChatManager (é obrigatório).");
            return;
        }

        string msg = inputField.text?.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        photonView.RPC(nameof(ReceiveMessage), RpcTarget.All, PhotonNetwork.NickName, msg);

        inputField.text = string.Empty;
        inputField.ActivateInputField();
    }

    [PunRPC]
    private void ReceiveMessage(string sender, string message)
    {
        var go = Instantiate(messagePrefab, messagesContent);
        if (!go.activeSelf) go.SetActive(true); // se usares o MESSAGEITEM como template desativado

        var label = go.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = $"<b>{sender}:</b> {message}";
        else Debug.LogError("[Chat] O Message Prefab não tem TMP_Text (nem nos filhos).");

        // auto-scroll para o fundo
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}