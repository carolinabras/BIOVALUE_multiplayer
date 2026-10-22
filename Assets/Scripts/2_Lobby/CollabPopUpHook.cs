using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The card owner's "X wants to collaborate on your action" card (mockup "Collaboration round"), in the
// bottom-right corner; several requests stack upwards. Built by "BioValue/Build Collaboration Request".
public class CollabPopUpHook : MonoBehaviour
{
    [SerializeField] private TMP_Text collabRequestText; // old pop-up only
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;
    [SerializeField] private Image avatar;
    [SerializeField] private TMP_Text avatarNumber;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text sub;
    [SerializeField] private TMP_Text cardTitle;
    [SerializeField] private TMP_Text cardDescription;
    [SerializeField] private float stackGap = 16f;

    private int _collaboratorActorNumber;
    private int _cardOwnerActorNumber;
    private int _cardIndex;

    public void Setup(string requesterName, string entity, string cardName, string description,
                      int collaboratorActorNumber, int cardOwnerActorNumber, int cardIndex)
    {
        _collaboratorActorNumber = collaboratorActorNumber;
        _cardOwnerActorNumber = cardOwnerActorNumber;
        _cardIndex = cardIndex;

        if (collabRequestText)
            collabRequestText.text = $"{requesterName} from {entity} wants to collaborate on this action: {cardName}. Do you accept?";
        int number = TurnRibbon.PlayerNumberOf(collaboratorActorNumber);
        if (avatar) avatar.color = PlayerCapsuleHook.ColorFor(Mathf.Max(1, number));
        if (avatarNumber) avatarNumber.text = number.ToString();
        if (title) title.text = $"<noparse>{requesterName}</noparse> wants to collaborate on your action";
        if (sub) sub.text = string.IsNullOrWhiteSpace(entity) || entity == "—"
            ? "Spends 1 of their tokens"
            : $"<noparse>{entity.Trim()}</noparse> · spends 1 token";
        if (cardTitle) cardTitle.text = $"<noparse>{cardName}</noparse>";
        if (cardDescription)
            cardDescription.text = string.IsNullOrWhiteSpace(description) ? "No description." : $"<noparse>{description.Trim()}</noparse>";

        acceptButton.onClick.AddListener(OnAccept);
        declineButton.onClick.AddListener(OnDecline);

        // Stack above the requests already open.
        var rt = (RectTransform)transform;
        int below = FindObjectsByType<CollabPopUpHook>(FindObjectsSortMode.None).Length - 1;
        rt.anchoredPosition += new Vector2(0, below * (rt.rect.height + stackGap));
        transform.SetAsLastSibling();

        var group = GetComponent<CanvasGroup>();
        if (group)
        {
            group.alpha = 0f;
            LeanTween.alphaCanvas(group, 1f, 0.2f).setIgnoreTimeScale(true);
        }
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
