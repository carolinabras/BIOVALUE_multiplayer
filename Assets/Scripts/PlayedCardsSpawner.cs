using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class PlayedCardsSpawner : MonoBehaviourPun
{
     [SerializeField] GameObject ActionCardPrefab;
     
     [SerializeField] ActionCardsDatabase actionCardsDatabase;
     
     [SerializeField] GameObject spawnerTransform;
     
     [SerializeField] TMP_Text playerName;

     [SerializeField] private GameObject parent;
     
    
    public List<GameObject> spawnedCards = new List<GameObject>(); // público para o CollaborationManager
    public int actorNumber;

    public void SetupContent(int playerId)
    {
        actionCardsDatabase = ActionCardsDatabaseSession.Instance.SessionDb;
        actorNumber = playerId;

        Player targetPlayer = null;
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (actorNumber == player.ActorNumber)
            {
                targetPlayer = player;
                break;
            }
        }

        if (targetPlayer == null) return;

        if (targetPlayer.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var nameObject))
            playerName.text = nameObject as string;
        else
            playerName.text = $"Player {actorNumber}";
    }

    public void OnClickSpawnCards()
    {
        bool isGM = GameState.Instance.localPlayerIndex == 0;

        if (isGM)
            photonView.RPC(nameof(RPC_SpawnPlayerCards), RpcTarget.All);
        else
            SpawnPlayerCards();
    }

    [PunRPC]
    public void RPC_SpawnPlayerCards()
    {
        SpawnPlayerCards();
    }

    public void SpawnPlayerCards()
    {
        parent.SetActive(true);

        foreach (var card in spawnedCards)
            Destroy(card);
        spawnedCards.Clear();

        int id = actorNumber - 1;
        if (!GameState.Instance.playerActionCards.TryGetValue(id, out List<int> actionCards)) return;

        int length = actionCards.Count;
        for (int i = 0; i < length; i++)
        {
            ActionCard cardData = actionCardsDatabase.GetActionCardById(actionCards[i]);
            if (cardData == null) continue;

            GameObject card = Instantiate(ActionCardPrefab, spawnerTransform.transform);

            ActionCardsHook hook = card.GetComponent<ActionCardsHook>();
            if (hook != null) hook.SetActionCard(cardData);

            // setup do botão de colaboração com o index da carta
            CollaborateButton collab = card.GetComponent<CollaborateButton>();
            if (collab != null) collab.Setup(actorNumber, i);

            spawnedCards.Add(card);
        }
    }
}
