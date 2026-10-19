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
    private const char Sep = '\u001F'; // between the fields of an entry on the wire

    // What an entry is about: picks its icon, colour and filter in the Game Log drawer.
    public enum Kind
    {
        Note = 0,
        Turn = 1,
        Placed = 2,
        VotePassed = 3,
        VoteFailed = 4,
        Cards = 5,
        CollabAccepted = 6,
        CollabDeclined = 7,
        Round = 8,      // divider: a new round starts (text = round name)
    }

    public class Entry
    {
        public Kind kind;
        public string time;     // "HH:mm"
        public string text;     // TMP rich text, names in <b>
        public string detail;   // optional second line (vote tally, card names)

        public string Plain => $"[{time}] {StripTags(text)}{(string.IsNullOrEmpty(detail) ? "" : $" ({detail})")}";

        public string Encode() => $"{(int)kind}{Sep}{time}{Sep}{text}{Sep}{detail}";

        public static Entry Decode(string s)
        {
            var parts = s.Split(Sep);
            if (parts.Length == 4 && int.TryParse(parts[0], out int k))
                return new Entry { kind = (Kind)k, time = parts[1], text = parts[2], detail = parts[3] };
            // Plain "[HH:mm] message" from an older client.
            if (s.Length > 8 && s[0] == '[' && s[6] == ']')
                return new Entry { kind = Kind.Note, time = s.Substring(1, 5), text = s.Substring(8), detail = "" };
            return new Entry { kind = Kind.Note, time = "", text = s, detail = "" };
        }
    }

    public List<Entry> Entries { get; } = new List<Entry>();
    public UnityEvent<string> OnEntryAdded = new UnityEvent<string>(); // plain text (old log panel)
    public event Action<Entry> EntryAdded;
    public event Action HistoryReplaced;

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
    public void Log(Kind kind, string text, string detail = "")
    {
        var opts = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(EventLog, Make(kind, text, detail).Encode(), opts, SendOptions.SendReliable);
    }

    public void Log(string message) => Log(Kind.Note, message);

    // Adds an entry to the local log only — no network send.
    // Call from inside Photon RPCs that already execute on all clients,
    // so the entry appears everywhere without a redundant broadcast.
    public void AddEntryLocal(Kind kind, string text, string detail = "") => Append(Make(kind, text, detail));

    public void AddEntryLocal(string message) => AddEntryLocal(Kind.Note, message);

    // Master sends the full log history to a player who just rejoined.
    public void PushHistoryTo(Player target)
    {
        if (Entries.Count == 0) return;
        var opts = new RaiseEventOptions { TargetActors = new[] { target.ActorNumber } };
        PhotonNetwork.RaiseEvent(EventHistory, Entries.Select(e => e.Encode()).ToArray(), opts, SendOptions.SendReliable);
    }

    // Name in bold for an entry's text; the name itself is never read as tags.
    public static string B(string name) => $"<b><noparse>{name}</noparse></b>";

    // ── IOnEventCallback ─────────────────────────────────────────────────────

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == EventLog && photonEvent.CustomData is string entry)
        {
            Append(Entry.Decode(entry));
        }
        else if (photonEvent.Code == EventHistory && photonEvent.CustomData is string[] history)
        {
            Entries.Clear();
            foreach (var e in history)
            {
                var decoded = Entry.Decode(e);
                Entries.Add(decoded);
                OnEntryAdded.Invoke(decoded.Plain);
            }
            HistoryReplaced?.Invoke();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void Append(Entry entry)
    {
        Entries.Add(entry);
        OnEntryAdded.Invoke(entry.Plain);
        EntryAdded?.Invoke(entry);
    }

    private static Entry Make(Kind kind, string text, string detail) =>
        new Entry { kind = kind, time = DateTime.Now.ToString("HH:mm"), text = text ?? "", detail = detail ?? "" };

    private static string StripTags(string s) =>
        System.Text.RegularExpressions.Regex.Replace(s ?? "", "<[^>]*>", "");

    public static string GetPlayerName(int actorNumber)
    {
        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber != actorNumber) continue;
            if (p.IsMasterClient) return "GM"; // the GM has no name of their own
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
