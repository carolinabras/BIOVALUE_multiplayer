using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// The end-of-game score, used by the results card and the export. The GM builds it in the Lobby
// (Scoring step): which items count and how many points each is worth (a minus takes points away),
// and whether the two objective averages are multiplied or added. Saved as a room property;
// without one, the default formula:
//   avg game-objective rating × avg personal-objective rating + 2 × actions played
//   + collaborations made + collaborations received
public static class ScoreFormula
{
    // Objectives = avg game × avg personal (the two averages multiplied); the others are single numbers.
    public enum Item { Objectives, AvgGame, AvgPersonal, Placed, Accepted, Rejected, Actions, CollabsMade, CollabsReceived }

    public struct Term
    {
        public Item item;
        public float weight;
        public Term(Item item, float weight) { this.item = item; this.weight = weight; }
    }

    // What the GM picks: points per item (missing = off) and how the two averages combine.
    public class Setup
    {
        public bool multiply = true;
        public readonly Dictionary<Item, float> weights = new Dictionary<Item, float>();

        public Setup Copy()
        {
            var s = new Setup { multiply = multiply };
            foreach (var kv in weights) s.weights[kv.Key] = kv.Value;
            return s;
        }

        public bool SameAs(Setup other) =>
            other != null && multiply == other.multiply && weights.Count == other.weights.Count &&
            weights.All(kv => other.weights.TryGetValue(kv.Key, out float w) && Mathf.Approximately(w, kv.Value));
    }

    // Items the GM can switch on, in the order of the Lobby (Objectives is made from the two averages).
    public static readonly Item[] Choosable =
        { Item.AvgGame, Item.AvgPersonal, Item.Placed, Item.Accepted, Item.Rejected, Item.Actions, Item.CollabsMade, Item.CollabsReceived };

    // Points an item gets when switched on.
    public static float DefaultWeight(Item item) => item == Item.Rejected ? -1f : item == Item.Actions ? 2f : 1f;

    public static Setup DefaultSetup()
    {
        var s = new Setup { multiply = true };
        s.weights[Item.AvgGame] = 1f;
        s.weights[Item.AvgPersonal] = 1f;
        s.weights[Item.Actions] = 2f;
        s.weights[Item.CollabsMade] = 1f;
        s.weights[Item.CollabsReceived] = 1f;
        return s;
    }

    public static readonly Term[] Default = TermsOf(DefaultSetup());

    // "mul;AvgGame=1;AvgPersonal=1;Actions=2"
    public static string Encode(Setup setup)
    {
        var parts = new List<string> { setup.multiply ? "mul" : "add" };
        foreach (var item in Choosable)
            if (setup.weights.TryGetValue(item, out float w))
                parts.Add(item + "=" + w.ToString(CultureInfo.InvariantCulture));
        return string.Join(";", parts);
    }

    public static Setup Decode(string text)
    {
        if (string.IsNullOrEmpty(text)) return DefaultSetup();
        var s = new Setup();
        foreach (var part in text.Split(';'))
        {
            if (part == "mul") { s.multiply = true; continue; }
            if (part == "add") { s.multiply = false; continue; }
            var kv = part.Split('=');
            if (kv.Length == 2 && System.Enum.TryParse(kv[0], out Item item) &&
                float.TryParse(kv[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float w))
                s.weights[item] = w;
        }
        return s;
    }

    // The room's formula (set by the GM in the Lobby), else the default.
    public static Setup CurrentSetup =>
        PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BiovalueStatics.ScoreFormulaKey, out var v) && v is string text
            ? Decode(text) : DefaultSetup();

    public static Term[] Current => TermsOf(CurrentSetup);

    // Both averages multiplied → one Objectives term (its points = both items' points multiplied).
    public static Term[] TermsOf(Setup setup)
    {
        var terms = new List<Term>();
        bool game = setup.weights.TryGetValue(Item.AvgGame, out float wg);
        bool own = setup.weights.TryGetValue(Item.AvgPersonal, out float wo);
        if (game && own && setup.multiply) terms.Add(new Term(Item.Objectives, wg * wo));
        else
        {
            if (game) terms.Add(new Term(Item.AvgGame, wg));
            if (own) terms.Add(new Term(Item.AvgPersonal, wo));
        }
        foreach (var item in Choosable)
            if (item != Item.AvgGame && item != Item.AvgPersonal && setup.weights.TryGetValue(item, out float w))
                terms.Add(new Term(item, w));
        return terms.Where(t => !Mathf.Approximately(t.weight, 0f)).ToArray();
    }

    public static string Label(Item item) => item switch
    {
        Item.Objectives => "Avg game × avg personal objective",
        Item.AvgGame => "Average game-objective rating",
        Item.AvgPersonal => "Average personal-objective rating",
        Item.Placed => "Instruments placed",
        Item.Accepted => "Instruments accepted",
        Item.Rejected => "Instruments rejected",
        Item.Actions => "Actions played",
        Item.CollabsMade => "Collaborations made",
        Item.CollabsReceived => "Collaborations received",
        _ => item.ToString()
    };

    // One line under the name in the Lobby.
    public static string Description(Item item) => item switch
    {
        Item.AvgGame => "Everyone's 0–10 rating of the game objective, averaged · same for everyone",
        Item.AvgPersonal => "Everyone's 0–10 rating of their own objective, averaged · same for everyone",
        Item.Placed => "Every instrument they put on the board",
        Item.Accepted => "Their instruments the others agreed on",
        Item.Rejected => "Times their instrument was voted down · takes points away",
        Item.Actions => "Action cards they played",
        Item.CollabsMade => "Accepted requests on other players' actions",
        Item.CollabsReceived => "Players who joined their actions",
        _ => ""
    };

    // Short, for the legend under the results.
    public static string Legend(Item item) => item switch
    {
        Item.Objectives => "Objectives (shared)",
        Item.AvgGame => "Game objective (shared)",
        Item.AvgPersonal => "Personal objective (shared)",
        _ => Label(item)
    };

    // As written in the formula line.
    public static string Short(Item item) => item switch
    {
        Item.Objectives => "avg game objective × avg personal objective",
        Item.AvgGame => "avg game objective",
        Item.AvgPersonal => "avg personal objective",
        Item.Placed => "instruments placed",
        Item.Accepted => "instruments accepted",
        Item.Rejected => "instruments rejected",
        Item.Actions => "actions played",
        Item.CollabsMade => "collaborations made",
        Item.CollabsReceived => "collaborations received",
        _ => item.ToString()
    };

    public static Color ColorOf(Item item) => item switch
    {
        Item.Objectives => new Color32(0x6A, 0x55, 0xA8, 0xFF),
        Item.AvgGame => new Color32(0x6A, 0x55, 0xA8, 0xFF),
        Item.AvgPersonal => new Color32(0x9A, 0x86, 0xD0, 0xFF),
        Item.Placed => new Color32(0x22, 0x8A, 0xA2, 0xFF),
        Item.Accepted => new Color32(0x47, 0xB0, 0x5A, 0xFF),
        Item.Rejected => new Color32(0xD9, 0x3A, 0x3A, 0xFF),
        Item.Actions => new Color32(0xE9, 0x8A, 0x1F, 0xFF),
        Item.CollabsMade => new Color32(0x3E, 0x9C, 0x8F, 0xFF),
        Item.CollabsReceived => new Color32(0x7F, 0xC4, 0xB8, 0xFF),
        _ => Color.grey
    };

    // "Score = avg game objective × avg personal objective + 2 × actions played + …"
    public static string Describe(Term[] terms)
    {
        if (terms.Length == 0) return "Score = 0 (nothing switched on)";
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
        var votes = VotingManager.Instance;
        switch (item)
        {
            case Item.Objectives: return avgGame * avgPersonal;
            case Item.AvgGame: return avgGame;
            case Item.AvgPersonal: return avgPersonal;
            case Item.Placed: return votes ? votes.GetPlacedCount(p.ActorNumber) : 0;
            case Item.Accepted: return votes ? votes.GetAcceptedCount(p.ActorNumber) : 0;
            case Item.Rejected: return votes ? votes.GetRejectedCount(p.ActorNumber) : 0;
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
