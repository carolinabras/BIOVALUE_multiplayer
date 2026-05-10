using UnityEngine;

public class ObjectsManager : MonoBehaviour
{
    [SerializeField] private GameObject InstrumentSpawnerArea;
    [SerializeField] private GameObject ActionCardSpawnerArea;
    [SerializeField] private GameObject endTurnButton;
    [SerializeField] private GameObject resultText;

    public void Start()
    {
        GameState.Instance.onGamePhaseChanged.AddListener(OnGamePhaseChanged);
        var current = GameState.Instance.GetCurrentGamePhase();
        OnGamePhaseChanged(current != GameState.GamePhase.None ? current : GameState.GamePhase.InstrumentSelection);
    }

    public void OnGamePhaseChanged(GameState.GamePhase gamePhase)
    {
        if (gamePhase == GameState.GamePhase.InstrumentSelection)
        {
            InstrumentSpawnerArea.SetActive(true);
            ActionCardSpawnerArea.SetActive(false);
        }
        else
        {
            InstrumentSpawnerArea.SetActive(false);
            ActionCardSpawnerArea.SetActive(true);
            endTurnButton.SetActive(false);
        }
    }
}
