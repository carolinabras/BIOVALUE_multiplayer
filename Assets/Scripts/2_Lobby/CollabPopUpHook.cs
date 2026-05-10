using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CollabPopUpHook : MonoBehaviour
{
    [SerializeField] private TMP_Text collabRequestText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;

    private int _collaboratorActorNumber;
    private int _cardOwnerActorNumber;
    private int _cardIndex;

    public void Setup(string requesterName, string entity, string cardName, int collaboratorActorNumber, int cardOwnerActorNumber, int cardIndex)
    {
        collabRequestText.text = $"{requesterName} from {entity} wants to collaborate on this action: {cardName}. Do you accept?";
        _collaboratorActorNumber = collaboratorActorNumber;
        _cardOwnerActorNumber = cardOwnerActorNumber;
        _cardIndex = cardIndex;

        acceptButton.onClick.AddListener(OnAccept);
        declineButton.onClick.AddListener(OnDecline);
    }

    private void OnAccept()
    {
        ColaborationManager.Instance.ConfirmCollaboration(_collaboratorActorNumber, _cardOwnerActorNumber, _cardIndex);
        Destroy(gameObject);
    }

    private void OnDecline()
    {
        ColaborationManager.Instance.DenyCollaboration(_collaboratorActorNumber, _cardOwnerActorNumber, _cardIndex);
        Destroy(gameObject);
    }
}
