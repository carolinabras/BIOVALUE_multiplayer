using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

// Owns per-player action-card selections and their descriptions. GameState
// remains responsible for the actual network sync (persisting to room
// properties + broadcasting the RPC) and for cross-cutting concerns like
// logging/database lookups when cards change.
public class ActionCardSyncService
{
    public readonly Dictionary<int, List<int>> PlayerActionCards = new Dictionary<int, List<int>>();
    public readonly Dictionary<string, string> Descriptions = new Dictionary<string, string>();
    public readonly Dictionary<string, string> DescriptionsHow = new Dictionary<string, string>();

    public void RestoreFromRoomProperties()
    {
        if (!PhotonNetwork.InRoom) return;
        var props = PhotonNetwork.CurrentRoom.CustomProperties;

        // Restore action cards for every player slot.
        for (int i = 0; i < 8; i++)
        {
            string key = BiovalueStatics.ActionCardsKeyPrefix + i;
            if (props.TryGetValue(key, out var cards) && cards is int[] arr)
                PlayerActionCards[i] = arr.ToList();
        }
    }

    public void SetCards(int playerId, int[] actionCardIds) =>
        PlayerActionCards[playerId] = actionCardIds.ToList();

    public void SetDescription(int playerId, int cardId, string description) =>
        Descriptions[$"{playerId}_{cardId}"] = description;

    public void SetDescriptionHow(int playerId, int cardId, string descriptionHow) =>
        DescriptionsHow[$"{playerId}_{cardId}"] = descriptionHow;

    public string GetDescription(int playerId, int cardId) =>
        Descriptions.TryGetValue($"{playerId}_{cardId}", out string desc) ? desc : string.Empty;

    public string GetDescriptionHow(int playerId, int cardId) =>
        DescriptionsHow.TryGetValue($"{playerId}_{cardId}", out string desc) ? desc : string.Empty;

    public readonly Dictionary<string, int[]> Links = new Dictionary<string, int[]>();

    public void SetLinks(int playerId, int cardId, int[] instrumentIds) =>
        Links[$"{playerId}_{cardId}"] = instrumentIds ?? new int[0];

    public int[] GetLinks(int playerId, int cardId) =>
        Links.TryGetValue($"{playerId}_{cardId}", out int[] ids) ? ids : new int[0];

    // Played cards (player index, card id) linked to the instrument with this number.
    public List<(int playerId, int cardId)> CardsLinking(int instrumentId)
    {
        var result = new List<(int, int)>();
        foreach (var kv in Links)
        {
            if (!kv.Value.Contains(instrumentId)) continue;
            var parts = kv.Key.Split('_');
            if (parts.Length == 2 && int.TryParse(parts[0], out int p) && int.TryParse(parts[1], out int c))
                result.Add((p, c));
        }
        return result;
    }
}
