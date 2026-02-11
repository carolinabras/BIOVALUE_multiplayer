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
    
    
    [SerializeField] private InstrumentHook instrumentHook;
    
    [SerializeField] private Transform detailsParent;

    private void Awake()
    {
        if (!detailsParent)
        {
            detailsParent = GetComponentInParent<Canvas>().transform;
        }
    }

    private void Update()
    {
        if (!isPressed) return;

        timer += Time.deltaTime;
        

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
}