using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
public class CardSummarySpawner : MonoBehaviourPun
{
    [SerializeField] private GameObject actionCardPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private ActionCardsDatabase actionCardsDatabase;
    [SerializeField] private GameObject summaryPanel;

    private void Awake()
    {
        actionCardsDatabase = ActionCardsDatabaseSession.Instance.SessionDb;
    }

    public void OnClickShowSummary()
    {
        Debug.Log($"OnClickShowSummary: playerActionCards count={GameState.Instance.playerActionCards.Count}");
        photonView.RPC(nameof(RPC_SpawnSummary), RpcTarget.All);
    }

    [PunRPC]
    private void RPC_SpawnSummary()
    {
        summaryPanel.SetActive(true);

        foreach (Transform child in container)
            Destroy(child.gameObject);

        foreach (var kvp in GameState.Instance.playerActionCards)
        {
            int playerId = kvp.Key;
            int actorNumber = playerId + 1;
            List<int> cardIds = kvp.Value;

            for (int i = 0; i < cardIds.Count; i++)
            {
                ActionCard cardData = actionCardsDatabase.GetActionCardById(cardIds[i]);
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

                GameObject cardObj = Instantiate(actionCardPrefab, container);

                ActionCardsHook hook = cardObj.GetComponent<ActionCardsHook>();
                if (hook != null)
                {
                    hook.isPlayedreal = true;
                    hook.ownerPlayerId = playerId;
                    hook.SetActionCard(cardCopy);
                }

                CardSummaryHook summaryHook = cardObj.GetComponent<CardSummaryHook>();
                if (summaryHook != null)
                    summaryHook.Setup(actorNumber, i);
            }
        }
    }
}
