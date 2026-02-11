using Photon.Pun;
using TMPro;
using UnityEngine;

public class ObjectiveDetails : MonoBehaviourPun
{
    [SerializeField] private TMP_Text titleText;

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
    
}
