using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;

public class CollaborateButton : MonoBehaviourPun
{
    [SerializeField] private Button _button;
    [SerializeField] private GameObject collabPrefab;
    [SerializeField] private GameObject collabContainer;

    private int _cardOwnerActorNumber;

    public void Setup(int cardOwnerActorNumber)
    {
        _cardOwnerActorNumber = cardOwnerActorNumber;

        bool isGM = GameState.Instance.localPlayerIndex == 0;
        bool isOwnCard = cardOwnerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
        _button.gameObject.SetActive(!isOwnCard && !isGM);
    }

    public void OnClickCollaborate()
    {
        if (!PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(BiovalueStatics.CollabTokensKey, out var tokensObj))
            return;

        int tokens = (int)tokensObj;
        if (tokens <= 0)
        {
            Debug.LogWarning("Sem tokens de colaboração!");
            return;
        }

        // gasta 1 token
        var props = new Hashtable();
        props[BiovalueStatics.CollabTokensKey] = tokens - 1;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        Debug.LogWarning("tokens are equal to" + tokens + "of ACTOR" + PhotonNetwork.LocalPlayer.ActorNumber);

        photonView.RPC(nameof(RPC_ShowCollaboration), RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);

    }

    [PunRPC]
    private void RPC_ShowCollaboration(int collaboratorActorNumber)
    {
        string collaboratorName = $"Player {collaboratorActorNumber}";
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber == collaboratorActorNumber)
            {
                if (player.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var nameObj))
                    collaboratorName = nameObj as string;
                break;
            }
        }
        GameObject collab = Instantiate(collabPrefab, collabContainer.transform);
        CollaboratorHook hook = collab.GetComponent<CollaboratorHook>();
        if (hook != null)
        {
            hook.Setup(collaboratorName);
        }

    }
}
