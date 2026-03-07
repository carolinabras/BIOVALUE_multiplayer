using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;

public class PlayersBoardSpawner : MonoBehaviourPunCallbacks
{
    public List<PlayerCapsuleHook> playerCapsulesHooks = new List<PlayerCapsuleHook>();

    private void Start()
    {
        Invoke(nameof(RequestPopulate), 0.5f);
    }

    private void RequestPopulate()
    {
        if (photonView && photonView.IsMine)
        {
            var players = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
            int count = players.Length;
            int[] actorNumbers = players.Select(p => p.ActorNumber).ToArray();
            string[] names = players
                .Select(p =>
                {
                    if (p.CustomProperties != null &&
                        p.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out object v) &&
                        v != null)
                    {
                        var s = v.ToString();
                        if (!string.IsNullOrWhiteSpace(s)) return s;
                    }

                    return $"Player {p.ActorNumber}";
                })
                .ToArray();
            photonView.RPC(nameof(RPC_PopulateBoard), RpcTarget.All, count, actorNumbers, names);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) => RequestPopulate();
    public override void OnPlayerLeftRoom(Player otherPlayer) => RequestPopulate();

    [PunRPC]
    public void RPC_PopulateBoard(int count, int[] actorNumbers, string[] names)
    {
        for (int i = 0; i < playerCapsulesHooks.Count; i++)
        {
            if (playerCapsulesHooks[i] == null)
            {
                Debug.LogWarning($"Player Capsule Hook at index {i} is null. Skipping.");
                continue;
            }

            // Player 0 is the GM and is not displayed on the board, so we start from index 1
            int playerIndex = i+1;
            if (playerIndex >= count)
            {
                playerCapsulesHooks[i].gameObject.SetActive(false);
                continue;
            }
            
            playerCapsulesHooks[i].SetPlayerInfo(names[playerIndex], actorNumbers[playerIndex]);
        }
    }
}

