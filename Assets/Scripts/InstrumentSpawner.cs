using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class InstrumentSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject instrumentPrefab;
    [SerializeField] private InstrumentsDatabase instrumentsDatabase;

    public GameObject parentOfInstruments;
    [HideInInspector] public List<InstrumentHook> injectionStepHooks = new List<InstrumentHook>();

    [SerializeField] private PhotonView photonView;

    private bool _populated = false;

    private void Awake()
    {
        instrumentsDatabase = InstrumentDatabaseSession.Instance.SessionDb;
    }

    private void Start()
    {
        // Give room properties time to arrive, then spawn.
        Invoke(nameof(Populate), 1f);
    }

    // Called by the GM only. Reads selected instrument IDs from room custom
    // properties (written by the lobby) so every client uses the same list.
    public void Populate()
    {
        if (_populated) return;
        if (!photonView || !photonView.IsMine) return;

        if (!instrumentPrefab)
        {
            Debug.LogError("[InstrumentSpawner] instrumentPrefab not assigned.");
            return;
        }

        if (!parentOfInstruments)
            parentOfInstruments = gameObject;

        // ── Read selected IDs from room properties ────────────────────────────
        var props = PhotonNetwork.CurrentRoom?.CustomProperties;
        if (props == null ||
            !props.TryGetValue(BiovalueStatics.SelectedInstrumentsKey, out var raw) ||
            !(raw is int[] ids) || ids.Length == 0)
        {
            Debug.LogWarning("[InstrumentSpawner] No selected instruments in room properties yet.");
            return;
        }

        var selectedInstruments = new List<Instrument>();
        foreach (int id in ids)
        {
            var inst = instrumentsDatabase.GetInstrumentById(id);
            if (inst != null) selectedInstruments.Add(inst);
        }

        if (selectedInstruments.Count == 0)
        {
            Debug.LogWarning("[InstrumentSpawner] None of the stored IDs matched the local database.");
            return;
        }

        _populated = true;

        injectionStepHooks = UiUtils.FillContainerWithPrefab<InstrumentHook>(
            parentOfInstruments,
            instrumentPrefab,
            selectedInstruments.Count,
            (hook, i) =>
            {
                hook.SetInstrumentInNetwork(selectedInstruments[i]);
                RectTransform rt = hook.GetComponent<RectTransform>();
                if (rt) rt.localPosition = new Vector3(50, 50);
                return true;
            },
            false,
            true  // PhotonNetwork.Instantiate
        );
    }

    // Fallback: if room properties arrive after Start, retry spawn.
    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (_populated) return;
        if (!changedProps.ContainsKey(BiovalueStatics.SelectedInstrumentsKey)) return;
        Populate();
    }

    public void SpawnInstrumentById(int id)
    {
        var instrument = instrumentsDatabase.GetInstrumentById(id);
        if (instrument == null)
        {
            Debug.LogWarning($"[InstrumentSpawner] Instrument id {id} not found.");
            return;
        }

        var go = Instantiate(instrumentPrefab, parentOfInstruments.transform);
        go.GetComponent<InstrumentHook>()?.SetInstrument(instrument);
    }
}
