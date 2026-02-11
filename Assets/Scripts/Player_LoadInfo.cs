using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class Player_LoadInfo : MonoBehaviourPunCallbacks
{
    public void Start()
    {
        PrintPlayerInfo();
        PrintObjective();
    }
    
    private void PrintPlayerInfo() 
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            string name = GetString(p, BiovalueStatics.PlayerNameKey);
            string company = GetString(p, BiovalueStatics.PlayerCompanyKey);
            string objective = GetString(p, BiovalueStatics.PlayerObjectiveKey);
            Debug.Log(name + ", " + company + ", " + objective);
        }
    }
    
    private string GetString(Player player, string key)
    {
        return player.CustomProperties.TryGetValue(key, out var value) ? value as string : string.Empty;
    }
    
    private void PrintObjective()
    {
        string objective = PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BiovalueStatics.GameObjectiveKey, out var obj) ? obj as string : string.Empty;
        Debug.Log("Game Objective: " + objective);
    }
}
