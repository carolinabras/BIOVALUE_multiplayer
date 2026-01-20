using Photon.Pun;
using UnityEngine;

public class ChecksPlayer : MonoBehaviour
{
    [SerializeField] public GameObject deck;
    [SerializeField] public GameObject panelGM;

    public bool isGM;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       if (PhotonNetwork.IsMasterClient)
       {
              Debug.Log("I am the Master Client (GM).");
              deck.SetActive(false);
              panelGM.SetActive(true);
       }
       else
       {
           Debug.Log("I am a regular client.");
       }
    }

    public void CheckRole()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            isGM = true;
            
        }
        else
        {
            isGM = false;
        }
    }
}
