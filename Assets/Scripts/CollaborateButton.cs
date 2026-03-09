using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;

public class CollaborateButton : MonoBehaviourPun
{
    [SerializeField] private Button button;
    [SerializeField] private GameObject collaboratorNamePrefab;
    [SerializeField] private Transform collaborateContainer;

    private int _cardOwnerActorNumber;
    private int _cardIndex;
    private bool _hasCollaborated = false;
    private GameObject _myCollabInstance;

    public void Setup(int cardOwnerActorNumber, int cardIndex)
    {
        _cardOwnerActorNumber = cardOwnerActorNumber;
        _cardIndex = cardIndex;

        bool isGM = GameState.Instance.localPlayerIndex == 0;
        bool isOwnCard = cardOwnerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
        
        Debug.Log($"Setup: cardOwnerActorNumber={cardOwnerActorNumber}, localActorNumber={PhotonNetwork.LocalPlayer.ActorNumber}, isGM={isGM}, isOwnCard={isOwnCard}, buttonActive={!isOwnCard && !isGM}");
        button.gameObject.SetActive(!isOwnCard && !isGM);
    }

    public void OnClickCollaborate()
    {
        
        if (_hasCollaborated)
            RemoveCollaboration();
        else
            AddCollaboration();
    }

    private void AddCollaboration()
    {
        bool hasKey = PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(BiovalueStatics.CollabTokensKey, out var tokenss);
    
        if (!hasKey) return;
        if (!PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(BiovalueStatics.CollabTokensKey, out var tokensObj)) return;

        int tokens = (int)tokensObj;
        if (tokens <= 0)
        {
            return;
        }

        // gasta 1 token
        var props = new Hashtable();
        props[BiovalueStatics.CollabTokensKey] = tokens - 1;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        _hasCollaborated = true;
        ColaborationManager.Instance.SendCollaboration(
            PhotonNetwork.LocalPlayer.ActorNumber,
            _cardOwnerActorNumber,
            _cardIndex
            
        );
    }

    private void RemoveCollaboration()
    {
        // devolve 1 token
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(BiovalueStatics.CollabTokensKey, out var tokensObj))
        {
            var props = new Hashtable();
            props[BiovalueStatics.CollabTokensKey] = (int)tokensObj + 1;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        _hasCollaborated = false;
        ColaborationManager.Instance.SendRemoveCollaboration(
            PhotonNetwork.LocalPlayer.ActorNumber,
            _cardOwnerActorNumber,
            _cardIndex
        );
    }

    private Dictionary<int, GameObject> _collabInstances = new Dictionary<int, GameObject>();

    public void ShowCollab(int collaboratorActorNumber)
    {
        // evita duplicados
        if (_collabInstances.ContainsKey(collaboratorActorNumber)) return;

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

        GameObject collab = Instantiate(collaboratorNamePrefab, collaborateContainer);
        collab.GetComponent<CollaboratorHook>()?.Setup(collaboratorName);
        _collabInstances[collaboratorActorNumber] = collab;
    }

    public void HideCollab(int collaboratorActorNumber)
    {
        if (_collabInstances.TryGetValue(collaboratorActorNumber, out GameObject collab))
        {
            Destroy(collab);
            _collabInstances.Remove(collaboratorActorNumber);
        }
    }
    }

