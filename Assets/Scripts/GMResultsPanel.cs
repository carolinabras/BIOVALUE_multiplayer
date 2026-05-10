using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class GMResultsPanel : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject playerResultPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private GameObject panel;

    public void OpenResults()
    {
        panel.SetActive(true);
        RefreshResults();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!panel.activeSelf) return;
        if (changedProps.ContainsKey(BiovalueStatics.MainObjectiveRatingKey) ||
            changedProps.ContainsKey(BiovalueStatics.PersonalObjectiveRatingKey) ||
            changedProps.ContainsKey(BiovalueStatics.PersonalObjectiveChangeKey))
        {
            RefreshResults();
        }
    }

    private void RefreshResults()
    {
        var allPlayers = PhotonNetwork.PlayerList
            .Where(p => !(p.IsLocal && p.IsMasterClient))
            .ToArray();

        // Sorted list used to map actor → playerActionCards index.
        var sorted = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();

        // Compute means across all players who have already submitted scores.
        var generalRatings  = new List<float>();
        var personalRatings = new List<float>();
        foreach (var player in allPlayers)
        {
            if (TryGetIntProp(player, BiovalueStatics.MainObjectiveRatingKey, out int g))
                generalRatings.Add(g);
            if (TryGetIntProp(player, BiovalueStatics.PersonalObjectiveRatingKey, out int p))
                personalRatings.Add(p);
        }

        float meanGeneral  = generalRatings.Count  > 0 ? generalRatings.Average()  : 0f;
        float meanPersonal = personalRatings.Count > 0 ? personalRatings.Average() : 0f;

        foreach (Transform child in container)
            Destroy(child.gameObject);

        foreach (var player in allPlayers)
        {
            string name = player.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var n)
                ? n as string : $"Player {player.ActorNumber}";

            bool hasMain     = TryGetIntProp(player, BiovalueStatics.MainObjectiveRatingKey,     out int mainRaw);
            bool hasPersonal = TryGetIntProp(player, BiovalueStatics.PersonalObjectiveRatingKey, out int personalRaw);
            string mainStr     = hasMain     ? mainRaw.ToString()     : "-";
            string personalStr = hasPersonal ? personalRaw.ToString() : "-";
            string change      = player.CustomProperties.TryGetValue(BiovalueStatics.PersonalObjectiveChangeKey, out var c)
                ? c as string : "-";

            // Index into playerActionCards (sorted position, 0 = GM).
            int sortedIndex  = System.Array.IndexOf(sorted, player);
            int actionCards  = GameState.Instance.playerActionCards.TryGetValue(sortedIndex, out var cards)
                ? cards.Count : 0;

            int collabsDone     = ColaborationManager.Instance != null
                ? ColaborationManager.Instance.GetCollaborationsDone(player.ActorNumber)     : 0;
            int collabsReceived = ColaborationManager.Instance != null
                ? ColaborationManager.Instance.GetCollaborationsReceived(player.ActorNumber) : 0;
            int disagrees       = VotingManager.Instance != null
                ? VotingManager.Instance.GetDisagreeCount(player.ActorNumber) : 0;

            // Formula: mean_general × mean_personal + 2 × actions + (collabs_done + collabs_received) + disagrees
            string scoreStr = (hasMain && hasPersonal)
                ? (meanGeneral * meanPersonal + 2f * actionCards + (collabsDone + collabsReceived) + disagrees).ToString("F1")
                : "-";

            var obj = Instantiate(playerResultPrefab, container);
            obj.GetComponent<PlayerResultHook>()?.Setup(name, mainStr, personalStr, change, scoreStr);
        }
    }

    private static bool TryGetIntProp(Player player, string key, out int value)
    {
        if (player.CustomProperties.TryGetValue(key, out var raw) && raw is int i)
        {
            value = i;
            return true;
        }
        value = 0;
        return false;
    }
}
