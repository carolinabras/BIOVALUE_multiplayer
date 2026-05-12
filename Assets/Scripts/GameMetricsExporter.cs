using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class GameMetricsExporter : MonoBehaviour
{
    public static GameMetricsExporter Instance;

    private void Awake() { Instance = this; }

    // Wire this to the export button's OnClick.
    public void Export()
    {
        var sb = new StringBuilder();
        var now = DateTime.Now;

        sb.AppendLine("========================================");
        sb.AppendLine("         BIOVALUE GAME METRICS");
        sb.AppendLine($"         {now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("========================================");
        sb.AppendLine();

        WritePlayerInfo(sb);
        WriteSelectedInstruments(sb);
        WritePlacedInstruments(sb);
        WriteActionCards(sb);
        WriteCollaborations(sb);
        WriteFinalResults(sb);

        string filename = $"BioValue_Metrics_{now:yyyyMMdd_HHmmss}.txt";
        string path = Path.Combine(Application.persistentDataPath, filename);
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);

        Debug.Log($"[GameMetricsExporter] Saved to: {path}");
    }

    // ── Sections ──────────────────────────────────────────────────────────────

    private void WritePlayerInfo(StringBuilder sb)
    {
        sb.AppendLine("--- PLAYERS ---");
        foreach (var player in NonGMPlayers())
        {
            string name      = Prop(player, BiovalueStatics.PlayerNameKey);
            string company   = Prop(player, BiovalueStatics.PlayerCompanyKey);
            string objective = Prop(player, BiovalueStatics.PlayerObjectiveKey);
            sb.AppendLine($"  {name}  |  Company: {company}  |  Objective: {objective}");
        }
        sb.AppendLine();
    }

    private void WriteSelectedInstruments(StringBuilder sb)
    {
        sb.AppendLine("--- INSTRUMENTS SELECTED BY GM ---");
        var props = PhotonNetwork.CurrentRoom?.CustomProperties;
        if (props != null &&
            props.TryGetValue(BiovalueStatics.SelectedInstrumentsKey, out var raw) &&
            raw is int[] ids && ids.Length > 0)
        {
            var db = InstrumentDatabaseSession.Instance?.SessionDb;
            foreach (int id in ids)
            {
                var inst = db?.GetInstrumentById(id);
                if (inst != null)
                    sb.AppendLine($"  [{id}] {inst.name}  ({inst.typeOne} / {inst.typeTwo})");
                else
                    sb.AppendLine($"  [{id}] Unknown");
            }
        }
        else
        {
            sb.AppendLine("  (none)");
        }
        sb.AppendLine();
    }

    private void WritePlacedInstruments(StringBuilder sb)
    {
        sb.AppendLine("--- INSTRUMENTS PLACED ON BOARD ---");
        var db = InstrumentDatabaseSession.Instance?.SessionDb;
        if (db == null) { sb.AppendLine("  (database unavailable)"); sb.AppendLine(); return; }

        var sorted = SortedPlayers();
        var placed = db.instruments.Where(i => i.isPlaced).ToList();

        if (placed.Count == 0)
        {
            sb.AppendLine("  (none)");
        }
        else
        {
            foreach (var inst in placed)
            {
                // ownerId is the sorted player index (0 = GM, 1+ = players).
                string owner = inst.ownerId >= 0 && inst.ownerId < sorted.Length
                    ? PlayerName(sorted[inst.ownerId].ActorNumber)
                    : $"Player {inst.ownerId}";
                sb.AppendLine($"  {inst.name}  →  placed by {owner}");
            }
        }
        sb.AppendLine();
    }

    private void WriteActionCards(StringBuilder sb)
    {
        sb.AppendLine("--- ACTION CARDS PLAYED ---");
        var gs = GameState.Instance;
        var db = ActionCardsDatabaseSession.Instance?.SessionDb;
        if (gs == null || db == null) { sb.AppendLine("  (data unavailable)"); sb.AppendLine(); return; }

        var sorted = SortedPlayers();
        foreach (var kvp in gs.playerActionCards.OrderBy(k => k.Key))
        {
            int idx = kvp.Key;
            string name = idx < sorted.Length ? PlayerName(sorted[idx].ActorNumber) : $"Player {idx}";
            var cardNames = kvp.Value.Select(id => db.GetActionCardById(id)?.cardName ?? $"Card {id}");
            sb.AppendLine($"  {name}: {string.Join(", ", cardNames)}");
        }
        sb.AppendLine();
    }

    private void WriteCollaborations(StringBuilder sb)
    {
        sb.AppendLine("--- COLLABORATIONS (collaborator → card ← owner) ---");
        var cm = ColaborationManager.Instance;
        var gs = GameState.Instance;
        var db = ActionCardsDatabaseSession.Instance?.SessionDb;

        if (cm == null || gs == null || db == null)
        { sb.AppendLine("  (data unavailable)"); sb.AppendLine(); return; }

        bool any = false;
        var sorted = SortedPlayers();

        foreach (var kvp in cm.collaborations)
        {
            if (kvp.Value.Count == 0) continue;

            // Key: "{ownerActorNumber}_{cardIndex}"
            var parts = kvp.Key.Split('_');
            if (parts.Length < 2) continue;
            if (!int.TryParse(parts[0], out int ownerActor) ||
                !int.TryParse(parts[1], out int cardIndex)) continue;

            string ownerName = PlayerName(ownerActor);

            int ownerSortedIdx = Array.FindIndex(sorted, p => p.ActorNumber == ownerActor);
            string cardName = "-";
            if (ownerSortedIdx >= 0 &&
                gs.playerActionCards.TryGetValue(ownerSortedIdx, out var cards) &&
                cardIndex < cards.Count)
            {
                cardName = db.GetActionCardById(cards[cardIndex])?.cardName ?? $"Card {cards[cardIndex]}";
            }

            foreach (int collabActor in kvp.Value)
            {
                sb.AppendLine($"  {PlayerName(collabActor)}  →  \"{cardName}\"  ←  {ownerName}");
                any = true;
            }
        }

        if (!any) sb.AppendLine("  (none)");
        sb.AppendLine();
    }

    private void WriteFinalResults(StringBuilder sb)
    {
        sb.AppendLine("--- FINAL RESULTS ---");
        var players = NonGMPlayers();
        var sorted  = SortedPlayers();
        var gs = GameState.Instance;
        var cm = ColaborationManager.Instance;
        var vm = VotingManager.Instance;

        // Means used in the score formula.
        var generalRatings  = new List<float>();
        var personalRatings = new List<float>();
        foreach (var p in players)
        {
            if (TryIntProp(p, BiovalueStatics.MainObjectiveRatingKey,     out int g)) generalRatings.Add(g);
            if (TryIntProp(p, BiovalueStatics.PersonalObjectiveRatingKey, out int pr)) personalRatings.Add(pr);
        }
        float meanG = generalRatings.Count  > 0 ? generalRatings.Average()  : 0f;
        float meanP = personalRatings.Count > 0 ? personalRatings.Average() : 0f;
        sb.AppendLine($"  Mean General Rating:  {meanG:F1}");
        sb.AppendLine($"  Mean Personal Rating: {meanP:F1}");
        sb.AppendLine();

        foreach (var player in players)
        {
            string name      = Prop(player, BiovalueStatics.PlayerNameKey);
            bool hasMain     = TryIntProp(player, BiovalueStatics.MainObjectiveRatingKey,     out int mainRaw);
            bool hasPersonal = TryIntProp(player, BiovalueStatics.PersonalObjectiveRatingKey, out int personalRaw);
            string change    = player.CustomProperties.TryGetValue(BiovalueStatics.PersonalObjectiveChangeKey, out var c)
                ? c as string ?? "-" : "-";

            int sortedIdx       = Array.IndexOf(sorted, player);
            int actionCards     = gs?.playerActionCards.TryGetValue(sortedIdx, out var cards) == true ? cards.Count : 0;
            int collabsDone     = cm?.GetCollaborationsDone(player.ActorNumber)     ?? 0;
            int collabsReceived = cm?.GetCollaborationsReceived(player.ActorNumber) ?? 0;
            int disagrees       = vm?.GetDisagreeCount(player.ActorNumber)          ?? 0;

            string score = (hasMain && hasPersonal)
                ? (meanG * meanP + 2f * actionCards + collabsDone + collabsReceived + disagrees).ToString("F1")
                : "-";

            sb.AppendLine($"  {name}");
            sb.AppendLine($"    General Rating:    {(hasMain     ? mainRaw.ToString()     : "-")}");
            sb.AppendLine($"    Personal Rating:   {(hasPersonal ? personalRaw.ToString() : "-")}");
            sb.AppendLine($"    Personal Change:   {change}");
            sb.AppendLine($"    Action Cards:      {actionCards}");
            sb.AppendLine($"    Collabs Done:      {collabsDone}");
            sb.AppendLine($"    Collabs Received:  {collabsReceived}");
            sb.AppendLine($"    Disagrees:         {disagrees}");
            sb.AppendLine($"    Score:             {score}");
        }
        sb.AppendLine();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Player[] NonGMPlayers() =>
        PhotonNetwork.PlayerList.Where(p => !p.IsMasterClient).OrderBy(p => p.ActorNumber).ToArray();

    private static Player[] SortedPlayers() =>
        PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();

    private static string PlayerName(int actorNumber)
    {
        foreach (var p in PhotonNetwork.PlayerList)
            if (p.ActorNumber == actorNumber)
            {
                string n = Prop(p, BiovalueStatics.PlayerNameKey);
                return n.Length > 0 ? n : $"Player {actorNumber}";
            }
        return $"Player {actorNumber}";
    }

    private static string Prop(Player player, string key) =>
        player.CustomProperties.TryGetValue(key, out var v) ? v as string ?? "" : "";

    private static bool TryIntProp(Player player, string key, out int value)
    {
        if (player.CustomProperties.TryGetValue(key, out var raw) && raw is int i)
        { value = i; return true; }
        value = 0;
        return false;
    }
}
