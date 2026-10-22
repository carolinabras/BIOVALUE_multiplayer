using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Finds an instrument piece by its number, with its category colours and its vote result.
// Used by the player details window and the collaboration round's instrument sheet.
public static class InstrumentLookup
{
    // The piece with this number: the one on the board first, else the one in the tray.
    public static InstrumentHook Find(int id) =>
        Object.FindObjectsByType<InstrumentHook>(FindObjectsSortMode.None)
            .Where(h => h.instrument != null && h.instrument.id == id)
            .OrderByDescending(h => h.instrument.isPlaced)
            .FirstOrDefault();

    // Its first and second category styles (second is null when it has only one).
    public static (InstrumentCategoryStyle a, InstrumentCategoryStyle b) Styles(InstrumentHook hook)
    {
        if (!hook || hook.instrument == null) return (null, null);
        var view = hook.GetComponent<InstrumentPieceView>();
        if (!view) return (null, null);
        var inst = hook.instrument;
        var a = view.StyleFor(inst.typeOne);
        var b = inst.typeTwo != inst.typeOne ? view.StyleFor(inst.typeTwo) : null;
        if (a == null) { a = b; b = null; }
        return (a, b);
    }

    public static string Categories(InstrumentHook hook)
    {
        var (a, b) = Styles(hook);
        var names = new List<string>();
        if (a != null && !string.IsNullOrEmpty(a.displayName)) names.Add(a.displayName);
        if (b != null && !string.IsNullOrEmpty(b.displayName)) names.Add(b.displayName);
        return string.Join(" · ", names);
    }

    // "Approved 3–0" when this client saw the vote, else "Approved" for a piece on the board (rejected ones go back).
    public static string VoteText(InstrumentHook hook)
    {
        if (!hook || hook.instrument == null || !hook.instrument.isPlaced) return "";
        return VotingManager.TryGetResult(hook.instrument.id, out int yes, out int no) && yes + no > 0
            ? $"Approved {yes}–{no}" : "Approved";
    }
}
