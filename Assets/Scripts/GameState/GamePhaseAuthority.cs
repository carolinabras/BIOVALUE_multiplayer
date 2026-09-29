using Photon.Pun;

// Owns the current game-phase value. GameState remains responsible for the
// actual network sync (persisting to room properties + broadcasting the RPC).
public class GamePhaseAuthority
{
    public GameState.GamePhase Current { get; private set; } = GameState.GamePhase.None;

    public void RestoreFromRoomProperties()
    {
        if (!PhotonNetwork.InRoom) return;
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        if (props.TryGetValue(BiovalueStatics.GamePhaseKey, out var phase))
            Current = (GameState.GamePhase)(int)phase;
    }

    // Returns true if the phase actually changed (false for a redundant set).
    public bool TrySet(GameState.GamePhase phase)
    {
        if (Current == phase) return false;
        Current = phase;
        return true;
    }
}
