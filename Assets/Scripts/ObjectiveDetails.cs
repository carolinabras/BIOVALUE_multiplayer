using Photon.Pun;
using TMPro;
using UnityEngine;

public class ObjectiveDetails : MonoBehaviourPun
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text linkText;

    public void SetObjective(string title)
    {
        if (titleText) titleText.text = title ?? "";
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
    
    [SerializeField] private UnityEngine.UI.Button linkButton;
    private string _link;

    public void SetLink(string link)
    {
        _link = link;
        if (linkText) linkText.text = string.IsNullOrEmpty(link) ? "" : link;
        if (linkButton) linkButton.gameObject.SetActive(!string.IsNullOrEmpty(link));
        linkButton.onClick.RemoveAllListeners();
        linkButton.onClick.AddListener(() => Application.OpenURL(_link));
    }
    
}
