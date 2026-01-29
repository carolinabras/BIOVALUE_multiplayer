using UnityEngine;

[CreateAssetMenu(fileName = "LobbyData", menuName = "Scriptable Objects/LobbyData")]
public class LobbyData : ScriptableObject
{
    public string objective;
    public string link;
    public int numPlayers = 2;
}
