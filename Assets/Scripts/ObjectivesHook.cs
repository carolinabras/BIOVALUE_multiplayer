using Photon.Pun;
using UnityEngine;

public class ObjectivesHook : MonoBehaviourPun
{
    [HideInInspector] public Objective objective;

    [SerializeField] private TMPro.TMP_Text nameText;
 

    public string Name
    {
        get => objective != null ? objective.name : string.Empty;
        set
        {
            if (objective != null)
            {
                objective.name = value;
            }

            if (nameText)
            {
                nameText.text = value;
            }
        }
    }
    
    public bool isSelected
    {
        get => objective != null && objective.isSelected;
        set
        {
            if (objective != null)
            {
                objective.isSelected = value;
            }
        }
    }

   /* public string Description
    {
        get => objective != null ? objective.description : string.Empty;
        set
        {
            if (objective != null)
            {
                objective.description = value;
            }

            if (descriptionText)
            {
                descriptionText.text = value;
            }
        }
    } */

    public void SetObjective(Objective obj)
    {
        objective = obj;
        if (obj == null)
        {
            Debug.LogError("Objective is null");
            return;
        }

        Name = objective.name;
        //Description = objective.description;
    }

    public void SetObjectiveNetwork(Objective obj)
    {
        if (photonView)
        {
            photonView.RPC(nameof(RPC_SetObjective), RpcTarget.All, obj.id, obj.name, obj.description);
        }
        else
        {
            Debug.LogWarning("PhotonView is not assigned, setting instrument locally.");
            SetObjective(obj);
        }
    }

    [PunRPC]
    public void RPC_SetObjective(int id, string objectiveName, string description)
    {
        Objective obj = new Objective
        {
            id = id,
            name = objectiveName,
            //description = description,
        };
        SetObjective(obj);
    }
}