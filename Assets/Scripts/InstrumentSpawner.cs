using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class InstrumentSpawner : MonoBehaviour
{
    [SerializeField] private GameObject instrumentPrefab;
    [SerializeField] private InstrumentsDatabase instrumentsDatabase;
    [SerializeField] private RectTransform mapGrid;

    public GameObject parentOfInstruments;
    [HideInInspector] public List<InstrumentHook> injectionStepHooks = new List<InstrumentHook>();

    [SerializeField] private PhotonView photonView;


    private void Start()
    {
        Invoke(nameof(PopulateRemote), 2.0f);
    }
    
    private void PopulateRemote()
    {
        if (!photonView) return;
        photonView.RPC(nameof(Populate), RpcTarget.AllBuffered);
    }

    [PunRPC]
    public void Populate()
    {
        if (!photonView) return;
        if (!photonView.IsMine) return;

        if (!instrumentsDatabase)
        {
            Debug.LogError("Instruments Database is not assigned.");
            return;
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
                instrumentsDatabase.instruments.Length, (hook, i) =>
                {
                    if (i >= instrumentsDatabase.instruments.Length)
                    {
                        Debug.LogError(
                            $"Expected {instrumentsDatabase.instruments.Length} instruments, but more {i} were provided.");
                        return false;
                    }

                    var instrument = instrumentsDatabase.instruments[i];

                    hook.SetInstrument(instrument, mapGrid);
                    RectTransform hookRect = hook.GetComponent<RectTransform>();
                    if (hookRect)
                    {
                        hookRect.localPosition = new Vector3(50, 50);
                    }

                    return true;
                }, true);
    }
}