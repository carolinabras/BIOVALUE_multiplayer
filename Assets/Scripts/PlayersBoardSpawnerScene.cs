using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayersBoardSpawnerScene : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject playersBoardPrefab;  // prefab do PlayersBoard (UI)
    [SerializeField] private Transform uiParent;             // Canvas / UI root onde queres o board
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

        // Mesma lógica: 1 autoridade decide
        if (!PhotonNetwork.IsMasterClient) return;

        pv.RPC(nameof(RPC_SpawnBoard), RpcTarget.AllBuffered);
        // Buffered para quem entrar depois também receber e instanciar o board
    }

    [PunRPC]
    private void RPC_SpawnBoard()
    {
        if (spawnedBoard != null) return;

        if (!playersBoardPrefab)
        {
            Debug.LogError("playersBoardPrefab não atribuído.");
            return;
        }

        if (!uiParent)
        {
            Debug.LogError("uiParent (Canvas/UI root) não atribuído.");
            return;
        }

        
    }
}

