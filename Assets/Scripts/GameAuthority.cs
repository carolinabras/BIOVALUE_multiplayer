using Photon.Pun;

// Single source of truth for "who is the GM" — the Master Client. Replaces the
// previously duplicated PhotonNetwork.IsMasterClient checks scattered across the
// project and the separate "role" custom-property check used in a couple of places.
public static class GameAuthority
{
    // Client-side check: is the local player the GM? Used to gate UI/buttons.
    public static bool IsGameMaster => PhotonNetwork.IsMasterClient;

    // Receiver-side check: did this RPC actually come from the GM? Use inside
    // [PunRPC] handlers for GM-only actions so a non-GM client calling the RPC
    // directly (bypassing the UI gate) is rejected rather than trusted.
    public static bool SenderIsGameMaster(PhotonMessageInfo info) =>
        info.Sender != null && info.Sender.IsMasterClient;
}
