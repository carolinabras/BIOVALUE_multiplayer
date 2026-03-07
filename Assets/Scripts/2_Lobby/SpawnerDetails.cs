using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class SpawnerDetails: MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler

{
    [SerializeField] private GameObject detailsPrefab;
    private DetailsHook detailsHook;
    
    [SerializeField] private float longPressDuration = 0.5f;
    private bool isPressed;
    private float timer;
    private bool spawned; 
    
    
    [SerializeField] private InstrumentHook instrumentHook;
    
    [SerializeField] private Transform detailsParent;

    private void Awake()
    {
        // if (!detailsParent)
        //     detailsParent = GetComponentInParent<Canvas>().transform;
        //
        // if (!instrumentHook)
        //     instrumentHook = GetComponentInParent<InstrumentHook>();
        if (!detailsParent)
        {
            var c = GetComponentInParent<Canvas>(true); // inclui inactivos
            if (c != null) detailsParent = c.transform;
            else
            {
                var anyCanvas = FindFirstObjectByType<Canvas>(); // Unity 6
                if (anyCanvas != null) detailsParent = anyCanvas.transform;
                else Debug.LogError("[SpawnerDetails] Não encontrei nenhum Canvas na cena.");
            }
        }
    }

    private void Update()
    {
        if (!isPressed || spawned) return;

        timer += Time.deltaTime;
        if (timer >= longPressDuration)
        {
            spawned = true;
            isPressed = false;
            SpawnDetails();
            Debug.Log("Long press detected, spawning details.");
        }
        
    }

    public void OnPointerDown(PointerEventData e)
    {
        timer = 0f;
        isPressed = true;
        Debug.Log("Pointer down detected, starting timer.");
        spawnedThisPress = false;
        spawned = false;
    }

    public void OnPointerUp(PointerEventData e)
    {
        isPressed = false;
        Debug.Log("Pointer up detected, resetting timer.");
    }
    public void OnPointerExit(PointerEventData e) => isPressed = false;

    public void SpawnDetails()
    {
        if (!instrumentHook)
        {
            Debug.LogError("InstrumentHook reference is not assigned.");
            return;
        }
        
        /*GameObject parent = GameObject.FindWithTag("DetailsParent");
        if (!parent)
        {
            Debug.LogError("Details parent not found in the scene.");
            return;
        } */

        GameObject detailsInstance = Instantiate(detailsPrefab, detailsParent, false);

        detailsHook = detailsInstance.GetComponent<DetailsHook>();
        if (!detailsHook)
        {
            Debug.LogError("DetailsHook component not found on the instantiated details prefab.");
            return;
        }

        Instrument instrument = instrumentHook.instrument;
        detailsHook.SetInstrumentDetails(instrument);
    }
    
    public void CancelLongPress()
    {
        isPressed = false;
        timer = 0f;
    }

    private bool spawnedThisPress = false;
}