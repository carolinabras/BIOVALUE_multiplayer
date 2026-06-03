using System.Collections.Generic;
using UnityEngine;

public class ActivateActionHandButton : MonoBehaviour
{
    [SerializeField] private GameObject button;
    public int targetPlayerIndex;

    private bool _cardActivated = false;

    public static readonly List<ActivateActionHandButton> All = new List<ActivateActionHandButton>();

    private void OnEnable()  { All.Add(this); }
    private void OnDisable() { All.Remove(this); }

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
        {
            _cardActivated = true;
            button.SetActive(true);
        }
    }

    public static void SetAllActive(bool active)
    {
        foreach (var b in All)
            if (b._cardActivated)
                b.button.SetActive(active);
    }
}
