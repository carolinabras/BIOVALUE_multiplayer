using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;
public class ObjectiveSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject objectivePrefab;
    public GameObject parentOfObjective;
    
    public PhotonView photonView;

    [HideInInspector] public List<ObjectiveHook> objectiveHooks = new List<ObjectiveHook>();

    private void Start()
    {
        Invoke(nameof(Populate), 0.5f); 
    }

    public void Populate()
    {
        if (!photonView) return;
        if (!photonView.IsMine) return;
        
        if (!objectivePrefab)
        {
            Debug.LogError("[ObjectiveSpawner] objectivePrefab não está atribuído.");
            return;
        }

        if (!parentOfObjective)
            parentOfObjective = this.gameObject;

        
        string objective = "";
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BiovalueStatics.GameObjectiveKey, out object v))
        {
            objective = v as string ?? "";
        }

        objectiveHooks =
            UiUtils.FillContainerWithPrefab<ObjectiveHook>(
                parentOfObjective,
                objectivePrefab,
                1,
                (hook, i) =>
                {
                    
                    // hook.SetObjective(objective);
                    hook.SetObjectiveInNetwork(objective);
                    return true;
                },
                false,
                true
            );

        Debug.Log($"[ObjectiveSpawner] Spawn objective com: '{objective}'");
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        // Se o objetivo chegar mais tarde / mudar, atualiza o hook existente
        if (changedProps != null && changedProps.ContainsKey(BiovalueStatics.GameObjectiveKey))
        {
            string objective = changedProps[BiovalueStatics.GameObjectiveKey] as string ?? "";

            foreach (var h in objectiveHooks)
                if (h != null) h.SetObjective(objective);

            Debug.Log($"[ObjectiveSpawner] Objective atualizado: '{objective}'");
        }
    }
}

