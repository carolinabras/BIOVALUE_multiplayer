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
        Invoke(nameof(Populate), 2.0f);
    }

    public void Populate()
    {
        if (!instrumentsDatabase)
        {
            instrumentsDatabase = GameKnowledge.Instance?.instrumentsDatabase;
            if (!instrumentsDatabase)
            {
                Debug.LogError("Instruments Database is not assigned.");
                return;
            }
        }

        if (!instrumentPrefab)
        {
            Debug.LogError("Protocol category or entry prefab is not assigned.");
            return;
        }

        if (!parentOfInstruments)
        {
            parentOfInstruments = this.gameObject;
        }

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
