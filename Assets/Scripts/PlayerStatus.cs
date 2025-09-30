using TMPro;
using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI playerName;
    [SerializeField] public TextMeshProUGUI status;
    
    public void Setup(string name, bool isReady)
    {
        playerName.text = name;
        if (isReady)
        {
            status.text = "Ready";
        }
        else
        {
            status.text = "Still not ready";
        }
    }
}

