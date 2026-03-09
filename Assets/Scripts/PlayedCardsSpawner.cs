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
        // fecha todos os outros spawners
        foreach (var spawner in FindObjectsOfType<PlayedCardsSpawner>())
        {
            if (spawner != this)
                spawner.ClosePanel();
        }

        SpawnPlayerCards();
        OpenPanel();
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
        LeanTween.cancel(parent);
        parent.GetComponent<RectTransform>().localScale = Vector3.zero;
        LeanTween.scale(parent.GetComponent<RectTransform>(), Vector3.one, 0.25f).setEaseOutBack();
    }

    public void ClosePanel()
    {
        if (parent == null || !parent.activeSelf) return;
        LeanTween.cancel(parent);
        parent.GetComponent<RectTransform>().localScale = Vector3.one;
        LeanTween.scale(parent.GetComponent<RectTransform>(), Vector3.zero, 0.25f)
            .setEaseInBack()
            .setOnComplete(() => parent.SetActive(false));
    }
}
