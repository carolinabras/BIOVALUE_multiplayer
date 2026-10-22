using Photon.Pun;
using Photon.Realtime;

// Single source of truth for "who is the GM". The GM is whoever started the game: their actor number is
// kept in a room property (GmActorKey, written by the Lobby when the game starts). Photon makes another
// player the master client while the GM is reconnecting, so the master client alone can't say who the GM
// is; before the game starts (no property yet) the master client is the GM.
public static class GameAuthority
{
    public const string GmActorKey = "gm_actor"; // int: actor number of the GM

    // The GM's actor number, or -1 before the game started.
    public static int GmActor =>
        PhotonNetwork.InRoom &&
        PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(GmActorKey, out var v) && v is int n ? n : -1;

    // Is this player the GM?
    public static bool IsGm(Player player)
    {
        if (player == null) return false;
        int gm = GmActor;
        return gm > 0 ? player.ActorNumber == gm : player.IsMasterClient;
    }

    // The GM (also while they're reconnecting: Photon keeps an inactive player in the list).
    public static Player GmPlayer
    {
        get
        {
            if (!PhotonNetwork.InRoom) return null;
            int gm = GmActor;
            return gm > 0 ? PhotonNetwork.CurrentRoom.GetPlayer(gm) : PhotonNetwork.MasterClient;
        }
    }

    // Client-side check: is the local player the GM? Used to gate UI/buttons.
    public static bool IsGameMaster => IsGm(PhotonNetwork.LocalPlayer);

    // Receiver-side check: did this RPC actually come from the GM? Use inside
    // [PunRPC] handlers for GM-only actions so a non-GM client calling the RPC
    // directly (bypassing the UI gate) is rejected rather than trusted.
    public static bool SenderIsGameMaster(PhotonMessageInfo info) => IsGm(info.Sender);

    // Called by the GM as the game starts.
    public static void ClaimGameMaster()
    {
        if (!PhotonNetwork.InRoom) return;
        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
            { { GmActorKey, PhotonNetwork.LocalPlayer.ActorNumber } });
    }
}
