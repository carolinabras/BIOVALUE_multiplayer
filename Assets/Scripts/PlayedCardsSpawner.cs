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

     [SerializeField] public GameObject parent;
     
    
   public List<GameObject> spawnedCards = new List<GameObject>();
    public int actorNumber;

    public void SetupContent(int playerId)
    {
        actionCardsDatabase = ActionCardsDatabaseSession.Instance.SessionDb;
        actorNumber = playerId;

        ActivateActionHandButton activateButton = GetComponent<ActivateActionHandButton>();
        if (activateButton != null)
            activateButton.targetPlayerIndex = actorNumber - 1;

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
        if (PhotonNetwork.IsMasterClient)
            photonView.RPC(nameof(RPC_SpawnPlayerCards), RpcTarget.All);
        else
            SpawnPlayerCards();
    }

    [PunRPC]
    public void RPC_SpawnPlayerCards()
    {
        OpenPanel();
        // fecha todos os outros spawners imediatamente
        foreach (var spawner in FindObjectsOfType<PlayedCardsSpawner>())
        {
            if (spawner != this)
            {
                // destrói cartas imediatamente
                foreach (var card in spawner.spawnedCards)
                    Destroy(card);
                spawner.spawnedCards.Clear();
            
                // fecha painel imediatamente sem animação
                if (spawner.parent != null)
                {
                    LeanTween.cancel(spawner.parent);
                    spawner.parent.SetActive(false);
                }
            }
        }

        SpawnPlayerCards();
       
    }

    public void OnClickClosePanel()
    {
        if (PhotonNetwork.IsMasterClient)
            photonView.RPC(nameof(RPC_ClosePanel), RpcTarget.All);
        else
            ClosePanel();
    }

    [PunRPC]
    public void RPC_ClosePanel()
    {
        ClosePanel();
    }

    public void SpawnPlayerCards()
    {
        parent.SetActive(true);

        // atualiza o nome do owner
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber == actorNumber)
            {
                if (player.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var nameObj))
                    playerName.text = nameObj as string;
                else
                    playerName.text = $"Player {actorNumber}";
                break;
            }
        }

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

            ActionCard cardCopy = new ActionCard
            {
                id = cardData.id,
                cardName = cardData.cardName,
                descriptionGeneral = cardData.descriptionGeneral,
                descriptionHow = cardData.descriptionHow,
                type = cardData.type,
                icon = cardData.icon
            };

            GameObject card = Instantiate(ActionCardPrefab, spawnerTransform.transform);

            ActionCardsHook hook = card.GetComponent<ActionCardsHook>();
            if (hook != null)
            {
                hook.isPlayedreal = true;
                hook.ownerPlayerId = id;
                hook.SetActionCard(cardCopy);
            }

            CollaborateButton collab = card.GetComponent<CollaborateButton>();
            if (collab != null) collab.Setup(actorNumber, i);

            spawnedCards.Add(card);
        }
    }

    public void OpenPanel()
    {
        if (parent == null) return;
        parent.SetActive(true);
        
        // parent.GetComponent<RectTransform>().localScale = Vector3.zero;
        
    }

    public void ClosePanel()
    {
        if (parent == null || !parent.activeSelf) return;
        
        // parent.GetComponent<RectTransform>().localScale = Vector3.one;
        parent.SetActive(false);
    }
}
