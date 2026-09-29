using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Editor-only debug bootstrap. Pressing Play directly on MainGame (instead of
// going through Main Menu -> Loading -> Lobby) skips the scene that normally
// creates InstrumentDatabaseSession / ActionCardsDatabaseSession — both
// DontDestroyOnLoad singletons other scripts read from in their own Awake().
// Without them, GameState/InstrumentSpawner throw NullReferenceExceptions on
// scene load and nothing shows up. This recreates the missing singletons,
// pointed at the same ScriptableObject assets Main Menu uses, before any
// other script's Awake() runs.
//
// Never active in a build (guarded by UNITY_EDITOR — it depends on
// AssetDatabase, which only exists in the Editor).
public static class DebugSceneBootstrap
{
#if UNITY_EDITOR
    private const string InstrumentsDatabasePath = "Assets/ScriptableObjects/DefaultInstrumentsDatabase.asset";
    private const string ActionCardsDatabasePath  = "Assets/ScriptableObjects/ActionCardsDatabase.asset";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureSessionSingletons()
    {
        // Unconditional: if you started from Main Menu like normal, its own copies
        // will find Instance already set (by whichever creates first) and just
        // self-destroy via their existing dedupe check — harmless either way.
        EnsureInstrumentDatabaseSession();
        EnsureActionCardsDatabaseSession();
    }

    private static void EnsureInstrumentDatabaseSession()
    {
        if (InstrumentDatabaseSession.Instance != null) return;

        var db = AssetDatabase.LoadAssetAtPath<InstrumentsDatabase>(InstrumentsDatabasePath);
        if (db == null)
        {
            Debug.LogWarning($"[DebugSceneBootstrap] Could not find InstrumentsDatabase at {InstrumentsDatabasePath}.");
            return;
        }

        // Create inactive so Awake() doesn't fire until masterDatabase is set below.
        var go = new GameObject("[Debug] InstrumentDatabaseSession");
        go.SetActive(false);
        var session = go.AddComponent<InstrumentDatabaseSession>();
        SetPrivateField(session, "masterDatabase", db);
        go.SetActive(true);

        Debug.Log("[DebugSceneBootstrap] Created missing InstrumentDatabaseSession for direct-scene testing.");
    }

    private static void EnsureActionCardsDatabaseSession()
    {
        if (ActionCardsDatabaseSession.Instance != null) return;

        var db = AssetDatabase.LoadAssetAtPath<ActionCardsDatabase>(ActionCardsDatabasePath);
        if (db == null)
        {
            Debug.LogWarning($"[DebugSceneBootstrap] Could not find ActionCardsDatabase at {ActionCardsDatabasePath}.");
            return;
        }

        var go = new GameObject("[Debug] ActionCardsDatabaseSession");
        go.SetActive(false);
        var session = go.AddComponent<ActionCardsDatabaseSession>();
        SetPrivateField(session, "actionCardsDatabase", db);
        go.SetActive(true);

        Debug.Log("[DebugSceneBootstrap] Created missing ActionCardsDatabaseSession for direct-scene testing.");
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(target, value);
    }
#endif
}
