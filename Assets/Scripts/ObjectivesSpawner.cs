using System.Collections.Generic;
using UnityEngine;

public class ObjectivesSpawner : MonoBehaviour
{
    [SerializeField] private ObjectivesDatabase objectivesDatabase;
    [SerializeField] private GameObject objectivePrefab;
    [SerializeField] public GameObject objectivesParent;
    
    [HideInInspector] public List<ObjectivesHook> injectionHook= new List<ObjectivesHook>();
    

   
     
    private void Start()
    {
        Invoke(nameof(Populate), 2.0f);
    }

    public void Populate()
    {
        if (!objectivesDatabase)
        {
            objectivesDatabase = GameKnowledge.Instance?.objectivesDatabase;
            if (!objectivesDatabase)
            {
                Debug.LogError("objectivesDatabase is not assigned.");
                return;
            }
        }

        if (!objectivePrefab)
        {
            Debug.LogError("Protocol category or entry prefab is not assigned.");
            return;
        }

        if (!objectivePrefab)
        {
            objectivePrefab = this.gameObject;
        }

        injectionHook =
            UiUtils.FillContainerWithPrefab<ObjectivesHook>(objectivesParent, objectivePrefab,
                objectivesDatabase.objectives.Count, (hook, i) =>
                {
                    if (i >= objectivesDatabase.objectives.Count)
                    {
                        Debug.LogError(
                            $"Expected {objectivesDatabase.objectives.Count} instruments, but more {i} were provided.");
                        return false;
                    }

                    var objective = objectivesDatabase.objectives[i];
                    hook.SetObjective(objective);
                    return true;
                });
        
    }
}
