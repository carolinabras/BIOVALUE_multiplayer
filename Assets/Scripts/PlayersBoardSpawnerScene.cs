using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayersBoardSpawnerScene : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject playersBoardPrefab;  // prefab do PlayersBoard (UI)
    [SerializeField] private Transform uiParent;             // Canvas / UI root where the board goes
    [SerializeField] private PhotonView pv;

    private GameObject spawnedBoard;

    private void Start()
    {
        Invoke(nameof(RequestSpawnBoard), 0.2f);
    }

    private void RequestSpawnBoard()
    {
        if (!pv) pv = GetComponent<PhotonView>();
        if (!pv) { Debug.LogError("PlayersBoardSceneSpawner precisa de PhotonView."); return; }

        // Same logic: one authority decides
        if (!GameAuthority.IsGameMaster) return;

        pv.RPC(nameof(RPC_SpawnBoard), RpcTarget.AllBuffered);
        // Buffered so players who join later also receive it and spawn the board
    }

    [PunRPC]
    private void RPC_SpawnBoard()
    {
        if (spawnedBoard != null) return;

        if (!playersBoardPrefab)
        {
            Debug.LogError("playersBoardPrefab not assigned.");
            return;
        }

        if (!uiParent)
        {
            Debug.LogError("uiParent (Canvas/UI root) not assigned.");
            return;
        }

        
    }
}

