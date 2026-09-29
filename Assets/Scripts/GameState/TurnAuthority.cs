using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// Owns turn-order state and the "who goes next" decision (including the
// skip-set bookkeeping for late rejoiners). GameState remains responsible for
// the actual network sync (persisting to room properties + broadcasting the
// RPC) — this class only decides and remembers.
public class TurnAuthority
{
    public int CurrentActorNumber { get; private set; } = -1;

    public void RestoreFromRoomProperties()
    {
        if (!PhotonNetwork.InRoom) return;
        var props = PhotonNetwork.CurrentRoom.CustomProperties;

        if (props.TryGetValue(BiovalueStatics.TurnActorKey, out var turnActor))
        {
            CurrentActorNumber = (int)turnActor;
        }
        else
        {
            // Fresh game — default to the first non-GM player (sorted index 1).
            var sorted = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
            CurrentActorNumber = sorted.Length > 1 ? sorted[1].ActorNumber : -1;
        }
    }

    public void SetCurrentActor(int actorNumber) => CurrentActorNumber = actorNumber;

    public bool IsCurrentActor(int actorNumber) => CurrentActorNumber == actorNumber;

    // Resolves the next eligible actor after fromActorNumber, honoring the skip
    // set (rejoiners deferred to the end of the current cycle). Returns -1 if
    // there's no one to advance to (e.g. only the GM is in the room).
    public int ResolveNextActor(int fromActorNumber)
    {
        var sorted = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
        if (sorted.Length <= 1) return -1; // only GM left

        HashSet<int> skipSet = GetSkipActors();

        Player next = sorted.Skip(1)
            .FirstOrDefault(p => p.ActorNumber > fromActorNumber && !skipSet.Contains(p.ActorNumber));

        if (next == null)
        {
            // End of cycle — admit deferred players back into rotation.
            if (skipSet.Count > 0) ClearSkipActors();
            next = sorted.Skip(1).FirstOrDefault(); // wrap to first non-GM
        }

        return next?.ActorNumber ?? -1;
    }

    // Defers a rejoining player's turn until the current cycle completes, so
    // they don't jump the queue ahead of players who were already waiting.
    public void DeferToEndOfCycle(int actorNumber)
    {
        var skip = GetSkipActors();
        skip.Add(actorNumber);
        PhotonNetwork.CurrentRoom.SetCustomProperties(
            new Hashtable { { BiovalueStatics.SkipActorsKey, skip.ToArray() } });
    }

    private HashSet<int> GetSkipActors()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BiovalueStatics.SkipActorsKey, out var v) && v is int[] arr)
            return new HashSet<int>(arr);
        return new HashSet<int>();
    }

    private void ClearSkipActors()
    {
        PhotonNetwork.CurrentRoom.SetCustomProperties(
            new Hashtable { { BiovalueStatics.SkipActorsKey, new int[0] } });
    }
}
