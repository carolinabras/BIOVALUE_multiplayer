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
        
        // atualiza se alguma das keys mudou
        if (changedProps.ContainsKey(BiovalueStatics.MainObjectiveRatingKey) ||
            changedProps.ContainsKey(BiovalueStatics.PersonalObjectiveRatingKey) ||
            changedProps.ContainsKey(BiovalueStatics.PersonalObjectiveChangeKey))
        {
            RefreshResults();
        }
    }

    private void RefreshResults()
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);

        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.IsLocal && PhotonNetwork.IsMasterClient) continue;

            string name = player.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var n) ? n as string : $"Player {player.ActorNumber}";
            string mainRating = player.CustomProperties.TryGetValue(BiovalueStatics.MainObjectiveRatingKey, out var m) ? m as string : "-";
            string personalRating = player.CustomProperties.TryGetValue(BiovalueStatics.PersonalObjectiveRatingKey, out var p) ? p as string : "-";
            string change = player.CustomProperties.TryGetValue(BiovalueStatics.PersonalObjectiveChangeKey, out var c) ? c as string : "-";

            GameObject obj = Instantiate(playerResultPrefab, container);
            obj.GetComponent<PlayerResultHook>()?.Setup(name, mainRating, personalRating, change);
        }
    }
}
