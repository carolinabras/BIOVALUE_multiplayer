using System;
using System.Collections;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class PlayerCapsuleHook : MonoBehaviourPun
{
  [SerializeField] public TMP_Text _playerName;
  [SerializeField] private TMP_Text _playerID;
  [SerializeField] private GameObject _IsMyTurnIndicator;
  public int _id;
  
  [SerializeField] private PlayedCardsSpawner cardSpawner;

  public void Start()
  {
    if (cardSpawner == null)
    {
      cardSpawner = GetComponent<PlayedCardsSpawner>();
    }
  }

  public void SetPlayerInfo(string playerName, int playerID)
  {
    _id = playerID;
    if (_playerName) _playerName.text = playerName;
    if (_playerID) _playerID.text = (_id - 1).ToString();
    
    GameState.Instance.onPlayerTurnIndexChanged.AddListener(OnPlayerIndexChanged);
    OnPlayerIndexChanged(GameState.Instance.GetCurrentPlayerTurnIndex()); // chamada a funcao OnPLayerIndexChanged pq precisa de fazer um check inicial

    if (cardSpawner != null)
    {
      cardSpawner.SetupContent(_id);
    }
  }

  public void OnPlayerIndexChanged(int index)
  {
    if (_IsMyTurnIndicator == null)
    {
      return;
    }
    
    _IsMyTurnIndicator.SetActive(index == (_id - 1));
  }
  
  
  
  public void SetPlayerInfoInNetwork(string playerName, int playerID)
  {
    StartCoroutine(SetPlayerInfoInNetwork_Async(playerName, playerID));
  }
  
  private IEnumerator SetPlayerInfoInNetwork_Async(string playerName, int playerID)
  {
    yield return new WaitForSeconds(0.5f);
    if(photonView)
    {
      photonView.RPC(nameof(RPC_SetPlayerInfo), RpcTarget.All, playerName, playerID);
    }
    else
    {
      Debug.LogWarning("PhotonView is not assigned, setting player info locally.");
      SetPlayerInfo(playerName, playerID);
    }
  }
  
  

  [PunRPC]
  public void RPC_SetPlayerInfo(string playerName, int playerID)
  {
    SetPlayerInfo(playerName, playerID);
  }
}
