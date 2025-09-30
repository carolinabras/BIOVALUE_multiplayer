using UnityEngine;

public class PlayerProfile : MonoBehaviour
{
    
    public static PlayerProfile Instance { get; private set; }
    public string PlayerName { get; set; } = "Player";
    public string Role { get; set; } = "Player"; // "GM" ou "Player"

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
}
