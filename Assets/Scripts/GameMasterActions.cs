using Photon.Pun;
using UnityEngine;

public class GameMasterActions : MonoBehaviour
{

    [SerializeField] public GameObject button;
    [SerializeField] public GameObject deckActionCards;
    
    public void EndInstrumentPhase()
    {
        //play animation to end instrumentphase 
        //lock drag pieces 
        //show deck actioncards
        //show panel of instructions
        
        // if (!PhotonNetwork.IsMasterClient)
        // {
        //     deckActionCards.SetActive(true);
        // }
        
        PhotonView.Get(this).RPC(nameof(RPC_OnEndInstrumentPhase), RpcTarget.All, null);
    }
    
    public void ShowAllCards()
    {
        //show all cards to everyone
        deckActionCards.SetActive(false);
        
    }
    
    public void AnimationEndInstrumentPhase()
    {
        //animation to end instrumentphase 
    }

    [PunRPC]
    public void RPC_OnEndInstrumentPhase()
    {
        //play animation to end instrumentphase 
        //lock drag pieces 
        //show deck actioncards
        //show panel of instructions
        
        if (!PhotonNetwork.IsMasterClient)
        {
            deckActionCards.SetActive(true);
        }
    }
    
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
