using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class CollaborateButton : MonoBehaviourPun
{
    [SerializeField] private Button button;
    [SerializeField] private GameObject collaboratorNamePrefab;
    [SerializeField] private Transform collaborateContainer;

    private int _cardOwnerActorNumber;
    private int _cardIndex;

    private Dictionary<int, GameObject> _collabInstances = new Dictionary<int, GameObject>();

    // Collaborating is asked from the collaboration round (CollaborationRound), so the card's old
    // Collaborate button stays hidden; this only lists the card's collaborators.
    public void Setup(int cardOwnerActorNumber, int cardIndex)
    {
        _cardOwnerActorNumber = cardOwnerActorNumber;
        _cardIndex = cardIndex;
        if (button) button.gameObject.SetActive(false);
    }

    public void ShowCollab(int collaboratorActorNumber)
    {
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
