using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// The end-of-game score, used by the results card and the export.
// For now always the default formula:
//   avg game-objective rating × avg personal-objective rating + 2 × actions played
//   + collaborations made + collaborations received
// (the GM will pick the items and their points in the lobby later).
public static class ScoreFormula
{
    public enum Item { Objectives, Actions, CollabsMade, CollabsReceived }

    public struct Term
    {
        public Item item;
        public float weight;
        public Term(Item item, float weight) { this.item = item; this.weight = weight; }
    }

    public static readonly Term[] Default =
    {
        new Term(Item.Objectives, 1f),
        new Term(Item.Actions, 2f),
        new Term(Item.CollabsMade, 1f),
        new Term(Item.CollabsReceived, 1f),
    };

    public static Term[] Current => Default;

    public static string Label(Item item) => item switch
    {
        Item.Objectives => "Avg game × avg personal objective",
        Item.Actions => "Actions played",
        Item.CollabsMade => "Collaborations made",
        Item.CollabsReceived => "Collaborations received",
        _ => item.ToString()
    };

    // Short, for the legend under the results.
    public static string Legend(Item item) => item == Item.Objectives ? "Objectives (shared)" : Label(item);

    // As written in the formula line.
    public static string Short(Item item) => item switch
    {
        Item.Objectives => "avg game objective × avg personal objective",
        Item.Actions => "actions played",
        Item.CollabsMade => "collaborations made",
        Item.CollabsReceived => "collaborations received",
        _ => item.ToString()
    };

    public static Color ColorOf(Item item) => item switch
    {
        Item.Objectives => new Color32(0x6A, 0x55, 0xA8, 0xFF),
        Item.Actions => new Color32(0xE9, 0x8A, 0x1F, 0xFF),
        Item.CollabsMade => new Color32(0x3E, 0x9C, 0x8F, 0xFF),
        Item.CollabsReceived => new Color32(0x7F, 0xC4, 0xB8, 0xFF),
        _ => Color.grey
    };

    // "Score = avg game objective × avg personal objective + 2 × actions played + …"
    public static string Describe(Term[] terms)
    {
        var sb = new System.Text.StringBuilder("Score = ");
        for (int i = 0; i < terms.Length; i++)
        {
            float w = terms[i].weight;
            if (i > 0) sb.Append(w < 0 ? " − " : " + ");
            else if (w < 0) sb.Append("− ");
            float a = Mathf.Abs(w);
            if (!Mathf.Approximately(a, 1f)) sb.Append(a.ToString("0.#")).Append(" × ");
            sb.Append(Short(terms[i].item));
        }
        return sb.ToString();
    }

    public static Player[] Players() =>
        PhotonNetwork.PlayerList.Where(p => !p.IsMasterClient).OrderBy(p => p.ActorNumber).ToArray();

    public static bool TryRating(Player p, string key, out int value)
    {
        if (p.CustomProperties.TryGetValue(key, out var raw) && raw is int i) { value = i; return true; }
        value = 0;
        return false;
    }

    public static bool HasAnswered(Player p) =>
        TryRating(p, BiovalueStatics.MainObjectiveRatingKey, out _) &&
        TryRating(p, BiovalueStatics.PersonalObjectiveRatingKey, out _);

    // Averages over the players who have answered.
    public static void Averages(out float game, out float personal, out int answered)
    {
        var done = Players().Where(HasAnswered).ToArray();
        answered = done.Length;
        game = personal = 0f;
        if (answered == 0) return;
        foreach (var p in done)
        {
            TryRating(p, BiovalueStatics.MainObjectiveRatingKey, out int g);
            TryRating(p, BiovalueStatics.PersonalObjectiveRatingKey, out int o);
            game += g;
            personal += o;
        }
        game /= answered;
        personal /= answered;
    }

    // The raw number for one item (before its points).
    public static float Value(Item item, Player p, float avgGame, float avgPersonal)
    {
        var collab = ColaborationManager.Instance;
        switch (item)
        {
            case Item.Objectives: return avgGame * avgPersonal;
            case Item.Actions:
                var state = GameState.Instance;
                return state != null && state.playerActionCards.TryGetValue(TurnRibbon.PlayerNumberOf(p.ActorNumber), out var cards)
                    ? cards.Count : 0;
            case Item.CollabsMade: return collab ? collab.GetCollaborationsDone(p.ActorNumber) : 0;
            case Item.CollabsReceived: return collab ? collab.GetCollaborationsReceived(p.ActorNumber) : 0;
        }
        return 0;
    }

    // Null until the player has answered (the averages need their ratings).
    public static float? Score(Player p, float avgGame, float avgPersonal)
    {
        if (!HasAnswered(p)) return null;
        return Current.Sum(t => t.weight * Value(t.item, p, avgGame, avgPersonal));
    }
}
