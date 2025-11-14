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
    
    private void Update()
    {
        if (!isPressed) return;

        timer += Time.deltaTime;

        if (timer >= longPressDuration)
        {
            isPressed = false;
            SpawnDetails();
        }
    }

    public void OnPointerDown(PointerEventData e)
    {
        timer = 0f;
        isPressed = true;
    }

    public void OnPointerUp(PointerEventData e) => isPressed = false;
    public void OnPointerExit(PointerEventData e) => isPressed = false;

    public void SpawnDetails()
    {
        if (!instrumentHook)
        {
            Debug.LogError("InstrumentHook reference is not assigned.");
            return;
        }
        
        GameObject parent = GameObject.FindWithTag("DetailsParent");
        if (!parent)
        {
            Debug.LogError("Details parent not found in the scene.");
            return;
        }

        GameObject detailsInstance = Instantiate(detailsPrefab, parent.transform);

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