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

        // fallback: tenta encontrar qualquer Canvas na cena
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            detailsParent = canvas.transform;
            return;
        }

        Debug.LogError($"[ObjectiveDetails_Spawner] Não encontrei nenhum Canvas. " +
                       $"Mete o spawner dentro de um Canvas ou atribui detailsParent manualmente. Obj: {name}");
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
            Debug.LogError("[LongPressSpawnObjectiveDetails] detailsPrefab não atribuído.");
            return;
        }

        // (opcional) garante que só existe 1 aberto
        if (currentDetailsInstance) Destroy(currentDetailsInstance);

        currentDetailsInstance = Instantiate(detailsPrefab, detailsParent, false);

        var hook = currentDetailsInstance.GetComponent<ObjectiveDetails>();
        if (!hook)
        {
            Debug.LogError("[LongPressSpawnObjectiveDetails] ObjectiveDetailsHook não existe no prefab.");
            return;
        }

        // Aqui vais buscar o objetivo como quiseres:
        // Opção A) Se o objective está guardado na Room Property:
        string objective = "";
        if (Photon.Pun.PhotonNetwork.CurrentRoom != null &&
            Photon.Pun.PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BiovalueStatics.GameObjectiveKey, out var v))
        {
            objective = v as string ?? "";
        }

        // Opção B) Se queres ler do ObjectiveHook (se ele guardar o texto ou tiver getter):
        // string objective = objectiveHook != null ? objectiveHook.GetText() : "";

        hook.SetObjective(objective);
    
    }
}

