using System;
using UnityEngine;

public class ObjectsManager : MonoBehaviour
{
    [SerializeField] private GameObject InstrumentSpawnerArea;
    [SerializeField] private GameObject ActionCardSpawnerArea;
    
    [SerializeField] private PhaseAnimationsManager phaseAnimationsManager;
    [SerializeField] private GameObject endTurnButton;
    [SerializeField] private GameObject resultText;

    public void Start()
    {
        GameState.Instance.onGamePhaseChanged.AddListener(OnGamePhaseChanged);
        OnGamePhaseChanged(GameState.GamePhase.InstrumentSelection);
    }

    public void OnGamePhaseChanged(GameState.GamePhase gamePhase)
    {
        if (gamePhase == GameState.GamePhase.InstrumentSelection)
        {
            // play animation of instruments phase
            phaseAnimationsManager.OpenInstruments();
            InstrumentSpawnerArea.SetActive(true);
            ActionCardSpawnerArea.SetActive(false);
        }
        else
        {
            //play animation of actions phase
            phaseAnimationsManager.OpenActions();
            InstrumentSpawnerArea.SetActive(false);
            ActionCardSpawnerArea.SetActive(true);
            endTurnButton.SetActive(false);
            
        }
       
    }
}
