using Photon.Pun;
using UnityEngine;

public class SetPhotonParentOnInstantiation : MonoBehaviour, IPunInstantiateMagicCallback
{
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] instantiationData = info.photonView.InstantiationData;

        if (instantiationData == null)
        {
            Debug.LogError("No instantiation data provided.");
            return;
        }

        if (instantiationData.Length < 1)
        {
            Debug.LogError("Insufficient instantiation data provided.");
            return;
        }
        
        if (!(instantiationData[0] is int))
        {
            Debug.LogError("Expected first instantiation data to be an integer representing the parent PhotonView ID.");
            return;
        }
        
        int parentViewID = (int)instantiationData[0];
        PhotonView parentPhotonView = PhotonView.Find(parentViewID);
        if (!parentPhotonView)
        {
            Debug.LogError($"No PhotonView found with ID {parentViewID}.");
            return;
        }
        
        GameObject parent = parentPhotonView.gameObject;
        if (!parent)
        {
            Debug.LogError("Parent GameObject is null.");
            return;
        }
        
        transform.SetParent(parent.transform);
    }
}