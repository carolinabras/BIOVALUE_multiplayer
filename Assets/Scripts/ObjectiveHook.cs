using System.Collections;
using Photon.Pun;
using UnityEngine;

public class ObjectiveHook : MonoBehaviourPun
{
    [SerializeField] private TMPro.TMP_Text objectiveText;
    
    public void SetObjective(string objective)
    {
        if (objectiveText)
        {
            objectiveText.text = objective;
        }
    }
    public void SetObjectiveInNetwork(string obj)
    {
        StartCoroutine(SetObjectiveInNetwork_Async(obj));
    }
    
    private IEnumerator SetObjectiveInNetwork_Async(string obj)
    {
        yield return new WaitForSeconds(0.5f);
        if (photonView)
        {
            photonView.RPC(nameof(RPC_SetObjective), RpcTarget.All, obj);
        }
        else
        {
            Debug.LogWarning("PhotonView is not assigned, setting instrument locally.");
            SetObjective(obj);
        }
    }

    [PunRPC]
    public void RPC_SetObjective(string title)
    {
        SetObjective(title);
    }
    
}
