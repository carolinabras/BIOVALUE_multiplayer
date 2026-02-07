using System;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_LobbyData : MonoBehaviour
{
    [SerializeField] private LobbyData lobbyData;

    [SerializeField] private TMP_InputField objectiveInput;
    [SerializeField] private TMP_InputField linkInput;
    
    [SerializeField] private PlayerSelection playerSelection;
    
    [SerializeField] private InstrumentsDatabase instrumentsDatabase;
    
    public Image continueButtonImage;
    public GameObject continueButton;
    [SerializeField] private Sprite continueButtonEnabledSprite;
    [SerializeField] private Sprite continueButtonDisabledSprite;
    
    [SerializeField] private TextMeshProUGUI errorMessageText;
    
    public int errorCode = -1; // -1: no error, 0: empty objective, 1: not enough instruments, 2: both
    
    
    
    public void Start()
    {
        LoadDataToUI();
    }

    public void Update()
    {
        ButtonSave();
        //Debug.Log("Objective: " + lobbyData.objective + ", Link: " + lobbyData.link + ", NumPlayers: " + lobbyData.numPlayers);
        
        // check if player count changes 
        int count = playerSelection.GetSelectedPlayerCount();
        if (lobbyData.numPlayers != count)
        {
            SetNumPlayers(count);
           
        }
        
    }

    public void OnObjectiveChange()
    {
        lobbyData.objective = objectiveInput.text;
    }
    
    public void OnLinkChange()
    {
        lobbyData.link = linkInput.text;
    }
    
    public void OnPlayerCountChange()
    {
        int count = playerSelection.GetSelectedPlayerCount();
        SetNumPlayers(count);
    }
    
    private void SetNumPlayers(int count)
    {
        lobbyData.numPlayers = count;
    }

    private void LoadDataToUI()
    {
        objectiveInput.text = lobbyData.objective;
        linkInput.text = lobbyData.link; 
        playerSelection.UpdateButtonStates();
        Debug.Log("Loaded Lobby Data to UI" + lobbyData.objective + ", " + lobbyData.link + ", " + lobbyData.numPlayers);
        
    }
    
    

    public bool CheckConditions()
    {
        //check if objective is not empty
        
        
        
        if (string.IsNullOrWhiteSpace(lobbyData.objective))
        {
            //Debug.Log("Objective vazia.");
            errorCode = 0;
            return false;
        }
        
        
        //check if number of instruments is at least numPlayers * 2
        
        var instrumentsDb = GameKnowledge.Instance?.instrumentsDatabase;
        if (instrumentsDb == null)
        {
            //Debug.LogError("InstrumentsDatabase não encontrada em GameKnowledge.");
            return false;
        }
        
        int requiredInstruments = lobbyData.numPlayers * 2;
        int selectedInstruments = instrumentsDb.GetSelectedCount();
        
        if (selectedInstruments < requiredInstruments)
        {
            //Debug.Log($"Número insuficiente de instrumentos selecionados. Selecionados: {selectedInstruments}, Necessários: {requiredInstruments}");
            errorCode = 1;
            return false;
        }
        
        if ((selectedInstruments < requiredInstruments) && 
            (string.IsNullOrWhiteSpace(lobbyData.objective)))
        {
            errorCode = 2;
        }
        
        
        return true;

    }
    
    

    public void ButtonSave()
    {
        if (CheckConditions())
        {
            
            
           
            //change sprite 
            if (continueButtonImage != null && continueButtonEnabledSprite != null)
            {
                continueButtonImage.sprite = continueButtonEnabledSprite;
            }
            
            
            
        }
        else
        {
           
            //Debug.Log("Failed to save lobby data. Conditions not met.");
            //change sprite
            if (continueButtonImage != null && continueButtonDisabledSprite != null)
            {
                continueButtonImage.sprite = continueButtonDisabledSprite;
            }
        }
    }
    
    
    public void GoToNextPhase()
    {
        PhotonNetwork.LoadLevel("Role");
    }
    
    public bool Interactable()
    {
        return CheckConditions();
    }
    
    public void ContinueButtonClicked()
    {
        if (Interactable())
        {
            GoToNextPhase();
        }       
        else
        {
            MessageError(errorCode);
        }
    }
    
    public void MessageError( int error)
    {
        
        if (error == 0)
        {
            errorMessageText.text = "*Please write the objective.";
        }
        else if (error == 1)
        {
            errorMessageText.text = "*Please select enough instruments." + " (Min: " + (lobbyData.numPlayers * 2) + ")";
        }
        else if (error == 2)
        {
            errorMessageText.text = "*Please write the objective and select enough instruments." + " (Min: " + (lobbyData.numPlayers * 2) + ")";
        }
    }
    
    
}
