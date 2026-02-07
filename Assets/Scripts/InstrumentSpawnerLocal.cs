using System.Collections.Generic;
using UnityEngine;



public class InstrumentSpawnerLocal : MonoBehaviour
{
    [SerializeField] private GameObject instrumentPrefab;
    [SerializeField] private InstrumentsDatabase instrumentsDatabase;

    public GameObject parentOfInstruments;
    [HideInInspector] public List<InstrumentHook> injectionStepHooks = new List<InstrumentHook>();

   
   

    private void Start()
    {
        //disable dragging for local spawner
        instrumentPrefab.GetComponent<DragPiece>().enabled = false;
       
        if (InstrumentDatabaseSession.Instance == null)
        {
            Debug.LogError("InstrumentDatabaseSession.Instance is null. Ensure the session object exists before this scene.");
            return;
        }

        instrumentsDatabase = InstrumentDatabaseSession.Instance.SessionDb;
        Invoke(nameof(Populate), 0.5f);
        
    }
    /*private void OnEnable()
    {
        if (instrumentsDatabase != null)
            instrumentsDatabase.OnDatabaseChanged.AddListener(Populate);
        Populate();
    }

    private void OnDisable()
    {
        if (instrumentsDatabase != null)
            instrumentsDatabase.OnDatabaseChanged.RemoveListener(Populate);
    }
    */
    private void OnEnable()
    {
        var session = InstrumentDatabaseSession.Instance;
        if (session == null || session.SessionDb == null)
        {
            Debug.LogError("Session DB ainda não existe. Garante que o Session object é criado antes desta cena.");
            return;
        }

        instrumentsDatabase = session.SessionDb;

        instrumentsDatabase.OnDatabaseChanged.AddListener(Populate);
        Populate();
    }

    private void OnDisable()
    {
        if (instrumentsDatabase != null)
            instrumentsDatabase.OnDatabaseChanged.RemoveListener(Populate);
    }
    
    public void Populate()
    {
        if (instrumentsDatabase == null)
        {
            Debug.LogError("Populate chamado mas instrumentsDatabase é null.");
            return;
        }
        Debug.Log("Populate called. Count = " + instrumentsDatabase.instruments.Count);

        if (!instrumentPrefab)
        {
            Debug.LogError("Protocol category or entry prefab is not assigned.");
            return;
        }

        if (!parentOfInstruments)
        {
            parentOfInstruments = this.gameObject;
        }
        
        for (int i = parentOfInstruments.transform.childCount - 1; i >= 0; i--)
            Destroy(parentOfInstruments.transform.GetChild(i).gameObject);

        injectionStepHooks.Clear();

        injectionStepHooks =
            UiUtils.FillContainerWithPrefab<InstrumentHook>(parentOfInstruments, instrumentPrefab,
                instrumentsDatabase.instruments.Count, (hook, i) =>
                {
                    if (i >= instrumentsDatabase.instruments.Count)
                    {
                        Debug.LogError(
                            $"Expected {instrumentsDatabase.instruments.Count} instruments, but more {i} were provided.");
                        return false;
                    }

                    var instrument = instrumentsDatabase.instruments[i];
                    hook.SetInstrument(instrument);
                    return true;
                });
        
    }
}
