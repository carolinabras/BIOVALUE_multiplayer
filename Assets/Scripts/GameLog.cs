using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;

public class GameLog : MonoBehaviour, IOnEventCallback
{
    public static GameLog Instance { get; private set; }

    private const byte EventLog     = 99;
    private const byte EventHistory = 98;

    public List<string> Entries { get; } = new List<string>();
    public UnityEvent<string> OnEntryAdded = new UnityEvent<string>();

    private void Awake() { Instance = this; }

    private void OnEnable()
    {
        if (Application.isPlaying) PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        if (Application.isPlaying) PhotonNetwork.RemoveCallbackTarget(this);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    // Broadcasts a log entry to every client.
    // Call only from single-client code (button handlers, non-RPC methods).
    public void Log(string message)
    {
        var opts = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(EventLog, Timestamp(message), opts, SendOptions.SendReliable);
    }

    // Adds an entry to the local log only — no network send.
    // Call from inside Photon RPCs that already execute on all clients,
    // so the entry appears everywhere without a redundant broadcast.
    public void AddEntryLocal(string message) => Append(Timestamp(message));

    // Master sends the full log history to a player who just rejoined.
    public void PushHistoryTo(Player target)
    {
        if (Entries.Count == 0) return;
        var opts = new RaiseEventOptions { TargetActors = new[] { target.ActorNumber } };
        PhotonNetwork.RaiseEvent(EventHistory, Entries.ToArray(), opts, SendOptions.SendReliable);
    }

    // ── IOnEventCallback ─────────────────────────────────────────────────────

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == EventLog)
        {
            Append((string)photonEvent.CustomData);
        }
        else if (photonEvent.Code == EventHistory && photonEvent.CustomData is string[] history)
        {
            Entries.Clear();
            foreach (var e in history) Append(e);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void Append(string entry)
    {
        Entries.Add(entry);
        OnEntryAdded.Invoke(entry);
    }

    private static string Timestamp(string message) =>
        $"[{DateTime.Now:HH:mm}] {message}";

    public static string GetPlayerName(int actorNumber)
    {
        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber != actorNumber) continue;
            if (p.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var n) && n is string s && s.Length > 0)
                return s;
            return $"Player {actorNumber}";
        }
        return $"Player {actorNumber}";
    }

    // sortedIndex matches GameState.localPlayerIndex (0 = GM, 1+ = players).
    public static string GetPlayerNameByIndex(int sortedIndex)
    {
        var sorted = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
        if (sortedIndex < 0 || sortedIndex >= sorted.Length) return $"Player {sortedIndex}";
        return GetPlayerName(sorted[sortedIndex].ActorNumber);
    }
}
