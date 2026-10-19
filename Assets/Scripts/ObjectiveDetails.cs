using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The game objective, full size, with the link to the materials (mockup "Objective piece").
// Opened by clicking the objective piece (ObjectideDetails_Spawner).
// Built by "BioValue/Build Objective Piece".
public class ObjectiveDetails : MonoBehaviourPun
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text linkText;
    [SerializeField] private Button linkButton;
    [SerializeField] private RectTransform card;
    [SerializeField] private Button[] closeButtons;  // backdrop, ×, "Back to the board"

    private static readonly Color Pink = new Color32(0xFE, 0x49, 0xA4, 0xFF);
    private static readonly Color Disabled = new Color32(0xF1, 0xEF, 0xF6, 0xFF);
    private static readonly Color Faint = new Color32(0xA9, 0xA3, 0xBC, 0xFF);

    private string _link;

    private void Start()
    {
        if (closeButtons != null)
            foreach (var b in closeButtons) if (b) b.onClick.AddListener(DestroySelf);
        if (card)
        {
            card.localScale = Vector3.one * 0.92f;
            LeanTween.scale(card.gameObject, Vector3.one, 0.25f).setEaseOutBack().setIgnoreTimeScale(true);
        }
    }

    public void SetObjective(string title)
    {
        if (titleText) titleText.text = string.IsNullOrWhiteSpace(title) ? "No objective yet." : $"<noparse>{title.Trim()}</noparse>";
    }

    public void DestroySelf() => Destroy(gameObject);

    public void SetObjectiveInNetwork(Objective obj)
    {
        if (photonView)
        {
            photonView.RPC(nameof(RPC_SetObjective), RpcTarget.All, obj.description);
        }
        else
        {
            Debug.LogWarning("PhotonView is not assigned, setting instrument locally.");
            SetObjective(obj.description);
        }
    }

    [PunRPC]
    public void RPC_SetObjective(string title)
    {
        SetObjective(title);
    }

    // The materials button opens the link; without one it says so, greyed out.
    public void SetLink(string link)
    {
        _link = string.IsNullOrWhiteSpace(link) ? "" : link.Trim();
        bool has = _link.Length > 0;
        if (linkText)
        {
            linkText.text = has ? $"<noparse>{_link}</noparse>" : "";
            linkText.gameObject.SetActive(has);
        }
        if (!linkButton) return;
        linkButton.interactable = has;
        if (linkButton.targetGraphic) linkButton.targetGraphic.color = has ? Pink : Disabled;
        var label = linkButton.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (label)
        {
            label.text = has ? "Open the materials" : "No materials added";
            label.color = has ? Color.white : Faint;
        }
        var arrow = linkButton.transform.Find("Arrow");
        if (arrow) arrow.gameObject.SetActive(has);
        linkButton.onClick.RemoveAllListeners();
        linkButton.onClick.AddListener(OpenLink);
    }

    private void OpenLink()
    {
        if (string.IsNullOrEmpty(_link)) return;
        Application.OpenURL(_link.Contains("://") ? _link : "https://" + _link);
    }
}
