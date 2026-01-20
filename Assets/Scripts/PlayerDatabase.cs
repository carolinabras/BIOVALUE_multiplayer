using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerDatabase : MonoBehaviourPunCallbacks
{
    public static PlayerDatabase Instance;

    // actorNumber -> BiovaluePlayer
    public Dictionary<int, BiovaluePlayer> Players = new Dictionary<int, BiovaluePlayer>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        RefreshAllPlayers();
    }

    // ----------------------------------------------------
    // Recarrega todos os jogadores da room atual
    // ----------------------------------------------------
    public void RefreshAllPlayers()
    {
        Players.Clear();

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            Players[p.ActorNumber] = new BiovaluePlayer(p);
        }

        Debug.Log($"[PlayerDatabase] Atualizados {Players.Count} jogadores.");
    }

    // ----------------------------------------------------
    // Chamado pelo Photon quando um player entra
    // ----------------------------------------------------
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Players[newPlayer.ActorNumber] = new BiovaluePlayer(newPlayer);
        Debug.Log($"[PlayerDatabase] Jogador entrou: {newPlayer.NickName}");
    }

    // ----------------------------------------------------
    // Chamado pelo Photon quando um player sai
    // ----------------------------------------------------
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (Players.Remove(otherPlayer.ActorNumber))
        {
            Debug.Log($"[PlayerDatabase] Jogador saiu: {otherPlayer.NickName}");
        }
    }

    // ----------------------------------------------------
    // Chamado sempre que as CustomProperties de um player mudam
    // (ex: quando chamas SaveRole() no Role.cs)
    // ----------------------------------------------------
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // Recria o BiovaluePlayer com os dados atuais
        Players[targetPlayer.ActorNumber] = new BiovaluePlayer(targetPlayer);

        Debug.Log($"[PlayerDatabase] Propriedades atualizadas para jogador {targetPlayer.ActorNumber}.");
    }
}
