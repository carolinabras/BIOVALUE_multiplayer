using System.Linq;
using Photon.Pun;
using UnityEngine;

public class PlayerCapsuleSpawner : MonoBehaviour
{
    [SerializeField] private PlayerCapsuleUI playerCapsulePrefab;
    [SerializeField] private Transform container;   // onde vão aparecer (VerticalLayout, Grid, etc.)

    private void OnEnable()
    {
        RefreshCapsules();
    }

    public void RefreshCapsules()
    {
        // Limpa o que já existe
        if (container == null || playerCapsulePrefab == null || PlayerDatabase.Instance == null)
        {
            Debug.LogWarning("[PlayerCapsuleSpawner] Faltam referências.");
            return;
        }

        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // Ordena jogadores por ActorNumber (outra lógica se quiseres)
        var players = PlayerDatabase.Instance.Players.Values
            .OrderBy(p => p.ActorNumber)
            .ToList();

        for (int i = 0; i < players.Count; i++)
        {
            var biovaluePlayer = players[i];

            // número “humano” (1,2,3,4…)
            int playerNumber = i + 1;

            // nome – se o teu BiovaluePlayer.Name estiver vazio, usa o NickName do Photon
            string playerName = string.IsNullOrWhiteSpace(biovaluePlayer.Name)
                ? biovaluePlayer.Player?.NickName ?? $"Player {playerNumber}"
                : biovaluePlayer.Name;

            // Instanciar UI
            var capsule = Instantiate(playerCapsulePrefab, container);
            capsule.Setup(playerNumber, playerName);
        }
    }
}
