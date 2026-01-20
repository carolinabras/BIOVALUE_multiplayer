using Photon.Pun;
using TMPro;
using UnityEngine;

public class GameMasterActions : MonoBehaviour
{

    //[SerializeField] public GameObject button;
    //[SerializeField] public GameObject deckActionCards;
    [SerializeField] public TMP_InputField playerName;
    
    public ChecksPlayer checksPlayer;
    
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
        
        //PhotonView.Get(this).RPC(nameof(RPC_OnEndInstrumentPhase), RpcTarget.All, null);
    }
    
    /*
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
    } */
    

    public void KickPlayer()
    {
        if (playerName.text != "")
        {
            PhotonView.Get(this).RPC(nameof(RPC_KickPlayer), RpcTarget.MasterClient, playerName.text);
            Debug.Log("Kicking player: " + playerName.text);
        }
    }
    
    
    [PunRPC]
    public void RPC_KickPlayer(string cardName)
    {
        Debug.Log($"[GM] RPC_KickPlayer recebido para '{cardName}'");

        foreach (var player in PhotonNetwork.PlayerList)
        {
            string nameOnCard = "";
            if (player.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var n))
                nameOnCard = n as string ?? "";

            Debug.Log($"[GM] Comparar '{cardName}' com '{nameOnCard}' (actor {player.ActorNumber})");

            if (nameOnCard == cardName)
            {
                Debug.Log($"[GM] A expulsar jogador '{nameOnCard}' (actor {player.ActorNumber})");
                PhotonNetwork.CloseConnection(player);
                break;
            }
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
