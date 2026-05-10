using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GM_StatusRoleScreen : MonoBehaviourPunCallbacks
{
    
    [SerializeField] private Transform content;            // o content do ScrollView ou layout
    [SerializeField] private GameObject playerItemPrefab;  // o PlayerStatusItem prefab

    private readonly Dictionary<int, GameObject> itemsByActor = new();

    private void Start()
    {
        
            foreach (var player in PhotonNetwork.PlayerList)
                AddOrUpdate(player);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) => AddOrUpdate(newPlayer);

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (itemsByActor.TryGetValue(otherPlayer.ActorNumber, out var item))
        {
            Destroy(item);
            itemsByActor.Remove(otherPlayer.ActorNumber);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        
        foreach (var key in changedProps.Keys)
        {
            if (key is string s && (s == BiovalueStatics.PlayerReadyKey || s == BiovalueStatics.PlayerNameKey))
            {
                AddOrUpdate(targetPlayer);
                break;
            }
        }
    }

    private void AddOrUpdate(Player player)
    {
        
        if (player.IsLocal && PhotonNetwork.IsMasterClient)
        {
            return; // dont show GM on the list
        }
        
        if (!itemsByActor.TryGetValue(player.ActorNumber, out var item))
        {
            item = Instantiate(playerItemPrefab, content);
            itemsByActor[player.ActorNumber] = item;
        }

        string name = player.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var n) ? n as string : $"Jogador {player.ActorNumber-1}";
        bool isReady = player.CustomProperties.TryGetValue(BiovalueStatics.PlayerReadyKey, out var r) && r is bool b && b;

        item.GetComponent<PlayerStatus>().Setup(name, isReady);
    }

    private const string NextSceneKey = "nextScene";

    public void GoNext()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        PhotonNetwork.AutomaticallySyncScene = true;
        // Setting a room property fires OnRoomPropertiesUpdate on ALL clients,
        // including the master client itself — this is what was missing.
        PhotonNetwork.CurrentRoom.SetCustomProperties(
            new Hashtable { { NextSceneKey, "MainGame" } }
        );
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.TryGetValue(NextSceneKey, out var scene) && scene is string sceneName)
        {
            PhotonNetwork.LoadLevel(sceneName);
        }
    }
}
