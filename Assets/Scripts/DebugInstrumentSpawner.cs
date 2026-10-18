using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor/Development-Build-only debug helper (never active in a release
// build). Lets the GM spawn a batch of test instruments directly, bypassing
// the lobby's instrument-selection flow, so drag/board behavior can be
// tested solo in Play Mode without a multi-client build.
//
// Bootstraps itself — no scene or prefab changes needed to use it.
public class DebugInstrumentSpawner : MonoBehaviour
{
    private const int SpawnCount = 20;
    private bool _gmPanelReactivated;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!Debug.isDebugBuild) return; // Editor and Development Builds only

        var go = new GameObject("[Debug] InstrumentSpawner");
        go.AddComponent<DebugInstrumentSpawner>();
        DontDestroyOnLoad(go);
    }

    private void OnGUI()
    {
        // These debug tools only make sense in MainGame — stay invisible everywhere
        // else (Main Menu/Loading/Lobby all have their own real flow into a room,
        // and by the time a legitimate session reaches MainGame it's already in one).
        if (SceneManager.GetActiveScene().name != "MainGame") return;

        // Not connected/in a room yet (you pressed Play directly on MainGame instead
        // of going through Main Menu -> Loading -> Lobby): offer a one-click local
        // solo room so the rest of debug testing works without a real connection.
        if (!PhotonNetwork.InRoom)
        {
            if (GUI.Button(new Rect(10, 10, 240, 30), "DEBUG: Start Offline Solo Room (GM)"))
            {
                PhotonNetwork.OfflineMode = true;
                PhotonNetwork.CreateRoom("DebugSoloRoom");
                Debug.Log("[DebugInstrumentSpawner] Started offline solo room for debug testing.");
            }
            return;
        }

        if (!GameAuthority.IsGameMaster) return;

        ReactivateGmPanelIfNeeded();

        var spawner = FindFirstObjectByType<InstrumentSpawner>();
        if (spawner != null &&
            GUI.Button(new Rect(10, 10, 240, 30), $"DEBUG: Spawn {SpawnCount} instruments"))
        {
            spawner.DebugSpawnTestInstruments(SpawnCount);
            if (PhaseIntro.Instance) PhaseIntro.Instance.Replay(GameState.GamePhase.InstrumentSelection);
        }

        // The GM panel is a slide-out drawer normally opened by clicking its own
        // in-scene button — this gives a guaranteed way to open/close it for
        // testing regardless of where that button ends up at the current resolution.
        var gmPanel = FindFirstObjectByType<GameMasterPanelUI>();
        if (gmPanel != null &&
            GUI.Button(new Rect(10, 50, 240, 30), "DEBUG: Toggle GM Panel"))
            gmPanel.TogglePanel();
    }

    // GameMasterPanelUI.Start() deactivates its own GameObject if you weren't GM
    // yet at scene-load time — which is always true in the offline-solo-room debug
    // flow, since becoming GM happens later via a button click, not before Start()
    // runs. Reactivate it now that GM status is actually known, so it (and anything
    // that searches for it, like the button above) works again.
    private void ReactivateGmPanelIfNeeded()
    {
        if (_gmPanelReactivated) return;
        _gmPanelReactivated = true;

        var gmPanel = FindFirstObjectByType<GameMasterPanelUI>(FindObjectsInactive.Include);
        if (gmPanel != null && !gmPanel.gameObject.activeSelf)
        {
            gmPanel.gameObject.SetActive(true);
            Debug.Log("[DebugInstrumentSpawner] Reactivated GameMasterPanelUI (was deactivated before GM status was known).");
        }
    }
}
