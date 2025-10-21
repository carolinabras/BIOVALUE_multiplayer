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


    private void Start()
    {
        Invoke(nameof(Populate), 2.0f);
    }

    public void Populate()
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
                    
                    PhotonView hookPhotonView = hook.GetComponent<PhotonView>();
                    if (hookPhotonView)
                    {
                        if (hookPhotonView.ViewID != 0)
                        {
                            hookPhotonView.RPC(nameof(InstrumentHook.SetInstrumentRPC), RpcTarget.All, instrument.id);
                        }
                        else
                        {
                            StartCoroutine(SetInstrumentWhenValidViewId(hookPhotonView, instrument));
                        }
                    }
                    else
                    {
                        hook.SetInstrument(instrument);
                    }
                    RectTransform hookRect = hook.GetComponent<RectTransform>();
                    if (hookRect)
                    {
                        hookRect.localPosition = new Vector3(50, 50);
                    }

                    return true;
                }, false, true);
    }
    
    IEnumerator SetInstrumentWhenValidViewId(PhotonView hookPhotonView, Instrument instrument)
    {
        while (hookPhotonView.ViewID == 0)
        {
            yield return null; // Wait for the next frame
        }
        
        hookPhotonView.RPC("SetInstrumentRPC", RpcTarget.All, instrument.id);
    }
}