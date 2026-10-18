using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectideDetails_Spawner :  MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private GameObject detailsPrefab;
    private ObjectiveDetails detailsHook;
    
    [SerializeField] private float longPressDuration = 0.5f;
    private bool isPressed;
    private float timer;
    private GameObject currentDetailsInstance;
    
    [SerializeField] private ObjectiveHook objectiveHook;
    
    [SerializeField] private Transform detailsParent;

    private void Awake()
    {
        if (detailsParent) return;

        var canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
        {
            detailsParent = canvas.transform;
            return;
        }

        // fallback: try to find any Canvas in the scene
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            detailsParent = canvas.transform;
            return;
        }

        Debug.LogError($"[ObjectiveDetails_Spawner] No Canvas found. " +
                       $"Put the spawner inside a Canvas or assign detailsParent manually. Obj: {name}");
    }

    private void Update()
    {
        if (!isPressed) return;

        timer += Time.deltaTime;
        Debug.Log($"Timer: {timer:F2} / {longPressDuration} (isPressed: {isPressed})");
        

        if (timer >= longPressDuration)
        {
            
            SpawnDetails();
            Debug.Log("Long press detected, spawning details.");
        }
    }

    public void OnPointerDown(PointerEventData e)
    {
        timer = 0f;
        isPressed = true;
        Debug.Log("Pointer down detected, starting timer.");
    }

    public void OnPointerUp(PointerEventData e)
    {
        isPressed = false;
        Debug.Log("Pointer up detected, resetting timer.");
    }
    public void OnPointerExit(PointerEventData e) => isPressed = false;

    public void SpawnDetails()
    {
        if (!detailsPrefab)
        {
            Debug.LogError("[LongPressSpawnObjectiveDetails] detailsPrefab not assigned.");
            return;
        }

        // (optional) make sure only one is open
        if (currentDetailsInstance) Destroy(currentDetailsInstance);

        currentDetailsInstance = Instantiate(detailsPrefab, detailsParent, false);

        var hook = currentDetailsInstance.GetComponent<ObjectiveDetails>();
        if (!hook)
        {
            Debug.LogError("[LongPressSpawnObjectiveDetails] ObjectiveDetailsHook is missing from the prefab.");
            return;
        }

        // Get the objective here however you prefer:
        // Option A) If the objective is stored in the Room Property:
        string objective = "";
        if (Photon.Pun.PhotonNetwork.CurrentRoom != null &&
            Photon.Pun.PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BiovalueStatics.GameObjectiveKey, out var v))
        {
            objective = v as string ?? "";
        }

        // Option B) To read it from the ObjectiveHook (if it stores the text or has a getter):
        // string objective = objectiveHook != null ? objectiveHook.GetText() : "";

        hook.SetObjective(objective);
        string link = "";
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BiovalueStatics.LinkKey, out var l))
            link = l as string ?? "";

        hook.SetLink(link);
    
    }
}

