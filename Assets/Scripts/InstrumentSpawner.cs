using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class InstrumentSpawner : MonoBehaviour
{
    [SerializeField] private GameObject instrumentPrefab;
    [SerializeField] private InstrumentsDatabase instrumentsDatabase;

    public GameObject parentOfInstruments;
    [HideInInspector] public List<InstrumentHook> injectionStepHooks = new List<InstrumentHook>();

    [SerializeField] private PhotonView photonView;


    private void Awake()
    {
        instrumentsDatabase = InstrumentDatabaseSession.Instance.SessionDb;
    }
    
    private void Start()
    {
        Invoke(nameof(Populate), 0.5f);
    }

    /*public void Populate()
    {
        if (!photonView) return;
        if (!photonView.IsMine) return;

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
                    hook.SetInstrumentInNetwork(instrument);
                    
                    RectTransform hookRect = hook.GetComponent<RectTransform>();
                    if (hookRect)
                    {
                        hookRect.localPosition = new Vector3(50, 50);
                    }

                    return true;
                }, false, true);
    } */
    
    public void Populate()
    {
        if (!photonView) return;
        if (!photonView.IsMine) return;

        /*if (!instrumentsDatabase)
        {
            instrumentsDatabase = GameKnowledge.Instance?.instrumentsDatabase;
            if (!instrumentsDatabase)
            {
                Debug.LogError("Instruments Database is not assigned.");
                return;
            }
        }*/

        if (!instrumentPrefab)
        {
            Debug.LogError("Protocol category or entry prefab is not assigned.");
            return;
        }

        if (!parentOfInstruments)
            parentOfInstruments = this.gameObject;

        // ------------------------------
        // FILTRAR INSTRUMENTOS SELECIONADOS
        // ------------------------------
        List<Instrument> selectedInstruments = new List<Instrument>();
        foreach (var inst in instrumentsDatabase.instruments)
        {
            Debug.Log($"DB: {inst.name} isSelected={inst.isSelected} HASH={inst.GetHashCode()}");
            if (inst.isSelected)
                selectedInstruments.Add(inst);
        }

        // Se não houver selecionados, sair
        if (selectedInstruments.Count == 0)
        {
            Debug.LogWarning("Nenhum instrumento selecionado!");
            return;
        }

        // ------------------------------
        // SPAWN APENAS DOS SELECIONADOS
        // ------------------------------
        injectionStepHooks =
            UiUtils.FillContainerWithPrefab<InstrumentHook>(
                parentOfInstruments,
                instrumentPrefab,
                selectedInstruments.Count,
                (hook, i) =>
                {
                    var instrument = selectedInstruments[i];
                    hook.SetInstrumentInNetwork(instrument);

                    RectTransform hookRect = hook.GetComponent<RectTransform>();
                    if (hookRect)
                        hookRect.localPosition = new Vector3(50, 50);

                    return true;
                },
                false,
                true
            );
    }
    
    public void SpawnInstrumentById(int id)
    {

        Instrument instrument = instrumentsDatabase.GetInstrumentById(id);

        if (instrument == null)
        {
            Debug.LogWarning($"Instrument with id {id} not found");
            return;
        }


        GameObject goInstrument = Instantiate(instrumentPrefab, parentOfInstruments.transform);

        // meter os dados no hook
        InstrumentHook hook = goInstrument.GetComponent<InstrumentHook>();
        if (hook != null)
        {

            hook.SetInstrument(instrument);


            // hook.SetInstrumentInNetwork(instrument);
        }
    }
}