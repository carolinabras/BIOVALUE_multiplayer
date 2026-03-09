using UnityEngine;

public class ActivateActionHandButton : MonoBehaviour
{
    [SerializeField] private GameObject button;
    public int targetPlayerIndex;

    private void Start()
    {
        button.SetActive(false);
        GameState.Instance.onPlayerActionCardsSet.AddListener(OnCardsSet);
    }

    private void OnDestroy()
    {
        GameState.Instance.onPlayerActionCardsSet.RemoveListener(OnCardsSet);
    }

    private void OnCardsSet(int playerId)
    {
        if (playerId == targetPlayerIndex)
            button.SetActive(true);
    }
}
