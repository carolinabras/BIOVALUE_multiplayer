using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class PlayerSelection : MonoBehaviour
{

 public PlayerButton[] playerButtons;
    private int selectedPlayerCount = 2;

    private void Start()
    {
        UpdateButtonStates();
    }

    private void Awake()
    {
        foreach (var buttonGameObject in playerButtons)
        {
            if (buttonGameObject != null)
            {
                var buttonComponent = buttonGameObject.GetComponent<Button>();
            }
        }
    }

    public void SelectPlayerCount()
    {
        
        GameObject go = EventSystem.current.currentSelectedGameObject;
        if (go == null) return;

        
        PlayerButton button = go.GetComponent<PlayerButton>();
        if (button == null) return;

       
        selectedPlayerCount = button.playerCount;

       
        UpdateButtonStates();
    }

    public void UpdateButtonStates()
    {
        foreach (var button in playerButtons)
        {
            button.ToggleButton(button.playerCount <= selectedPlayerCount);
            
        }
    }
    
    
}

