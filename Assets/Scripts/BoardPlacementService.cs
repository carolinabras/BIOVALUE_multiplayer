// Coordinates what happens when a player places an instrument piece on the
// board: assign ownership and open the approval vote. DragPiece calls this
// single entry point instead of reaching into GameState and VotingManager
// separately for every placement.
public static class BoardPlacementService
{
    public static void RegisterPlacement(DragPiece piece, int newCellIndex, int oldCellIndex)
    {
        piece.GiveOwner(GameState.Instance.localPlayerIndex);
        VotingManager.Instance.StartVote(piece.photonView.ViewID, newCellIndex, oldCellIndex);
    }
}
