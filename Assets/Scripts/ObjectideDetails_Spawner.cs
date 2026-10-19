using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

// Click the objective piece: the objective details open (the objective in full and the link to the
// materials). A drag doesn't count as a click.
public class ObjectideDetails_Spawner : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject detailsPrefab;
    [SerializeField] private ObjectiveHook objectiveHook;
    [SerializeField] private Transform detailsParent;

    private GameObject currentDetailsInstance;

    private void Awake()
    {
        if (detailsParent) return;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            detailsParent = canvas.rootCanvas.transform; // over everything
            return;
        }

        Debug.LogError($"[ObjectiveDetails_Spawner] No Canvas found. " +
                       $"Put the spawner inside a Canvas or assign detailsParent manually. Obj: {name}");
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.dragging) return;
        SpawnDetails();
    }

    public void SpawnDetails()
    {
        if (!detailsPrefab)
        {
            Debug.LogError("[ObjectiveDetails_Spawner] detailsPrefab not assigned.");
            return;
        }

        // only one open at a time
        if (currentDetailsInstance) Destroy(currentDetailsInstance);

        currentDetailsInstance = Instantiate(detailsPrefab, detailsParent, false);
        currentDetailsInstance.transform.SetAsLastSibling();

        var hook = currentDetailsInstance.GetComponent<ObjectiveDetails>();
        if (!hook)
        {
            Debug.LogError("[ObjectiveDetails_Spawner] ObjectiveDetails is missing from the prefab.");
            return;
        }

        var room = PhotonNetwork.CurrentRoom;
        string objective = room != null && room.CustomProperties.TryGetValue(BiovalueStatics.GameObjectiveKey, out var v) ? v as string ?? "" : "";
        string link = room != null && room.CustomProperties.TryGetValue(BiovalueStatics.LinkKey, out var l) ? l as string ?? "" : "";
        hook.SetObjective(objective);
        hook.SetLink(link);
    }
}
