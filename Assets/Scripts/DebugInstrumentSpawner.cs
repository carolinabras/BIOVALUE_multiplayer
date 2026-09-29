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

        var spawner = FindFirstObjectByType<InstrumentSpawner>();
        if (spawner == null) return;

        if (GUI.Button(new Rect(10, 10, 240, 30), $"DEBUG: Spawn {SpawnCount} instruments"))
            spawner.DebugSpawnTestInstruments(SpawnCount);
    }
}
