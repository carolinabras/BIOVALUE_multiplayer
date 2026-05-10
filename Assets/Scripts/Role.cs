using System;
using System.Collections;
using Photon.Pun;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class Role : MonoBehaviour
{
    
    [SerializeField] public TMP_InputField company;
    [SerializeField] public TMP_InputField name;
    [SerializeField] public TMP_InputField personalObjective;
    
    [SerializeField] GameObject waitingForGMPanel;
    [SerializeField] TextMeshProUGUI waitingName;
    [SerializeField] TextMeshProUGUI waitingCompany;
    [SerializeField] TextMeshProUGUI waitingObjective;
    
    [SerializeField] private GameObject gmOverviewPanel;

    [SerializeField] private TextMeshProUGUI waiting;
    
    [SerializeField] private Image readyButton;
    [SerializeField] private Image readyIndicator;
    [SerializeField] private TextMeshProUGUI readyStatusText;

    private bool isReady = false;
    
    
    public int playerId;

    public void Start()
    {
        var me = PhotonNetwork.LocalPlayer;

        
        bool isGM = me.CustomProperties.TryGetValue("role", out var role) && (string)role == "GM";

        if (isGM)
        {
            // is gm 
            if (gmOverviewPanel != null)
                gmOverviewPanel.SetActive(true);
        }
        else
        {
            // is player 
            if (gmOverviewPanel!= null)
                gmOverviewPanel.SetActive(false);
        }
       

        Debug.Log($"[SceneController] Eu sou {(isGM ? "GM" : "Player")}, painel carregado.");

        if (readyIndicator == null)
            Debug.LogWarning("[Role] readyIndicator is not assigned in the Inspector!");
        else
            readyIndicator.color = Color.blue;
    }
    

    private void OnEnable()
    {
        var me = PhotonNetwork.LocalPlayer;
        if (me == null)
        {
            Debug.LogWarning("[Role] Ainda não estou ligado a uma room (LocalPlayer é null).");
            return;
        }

        Debug.Log($"[Role] A carregar dados para jogador ID={me.ActorNumber}...");

        if (me.CustomProperties.TryGetValue(BiovalueStatics.PlayerCompanyKey, out var c))
        {
            company.text = c as string ?? "";
            Debug.Log($"[Role] Empresa carregada: {company.text}");
        }
        else Debug.Log("[Role] Empresa não encontrada nas propriedades.");

        if (me.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var n))
        {
            name.text = n as string ?? "";
            Debug.Log($"[Role] Nome carregado: {name.text}");
        }
        else Debug.Log("[Role] Nome não encontrado nas propriedades.");

        if (me.CustomProperties.TryGetValue(BiovalueStatics.PlayerObjectiveKey, out var o))
        {
            personalObjective.text = o as string ?? "";
            Debug.Log($"[Role] Objetivo carregado: {personalObjective.text}");
        }
        else Debug.Log("[Role] Objetivo não encontrado nas propriedades.");
    }

    public void SaveRole()
    {
        string companyText = Safe(company?.text);
        string nameText = Safe(name?.text);
        string objectiveText = Safe(personalObjective?.text);

        var ht = new Hashtable
        {
            [BiovalueStatics.PlayerCompanyKey] = companyText,
            [BiovalueStatics.PlayerNameKey] = nameText,
            [BiovalueStatics.PlayerObjectiveKey] = objectiveText,
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(ht);

        Debug.Log("[Role] Dados guardados nas Custom Properties:");
        Debug.Log($"[Role] -> Empresa: {companyText}");
        Debug.Log($"[Role] -> Nome: {nameText}");
        Debug.Log($"[Role] -> Objetivo: {objectiveText}");
        
        
        waitingForGMPanel.SetActive(true);
        waitingName.text = "Name: " + nameText;
        waitingCompany.text = "Company: " + companyText;
        waitingObjective.text = "Objetive: " + objectiveText;
    }

    private static string Safe(string s) => string.IsNullOrWhiteSpace(s) ? "—" : s.Trim();
    
    public void setNotReady()
    {
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
        {
            { BiovalueStatics.PlayerReadyKey, false }
        });
        isReady = false;
        if (readyIndicator != null) readyIndicator.color = Color.blue;
        if (readyButton != null) readyButton.color = Color.white;
        if (readyStatusText != null) readyStatusText.text = "";

        Debug.Log("[Role] Estado PRONTO removido (entrou em modo edição)");
    }
    
    public void CloseGmePanel()
    {
        waitingForGMPanel.SetActive(false);
    }

    public void ToggleReady()
    {
        isReady = !isReady;

        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
        {
            [BiovalueStatics.PlayerReadyKey] = isReady
        });

        if (readyIndicator != null) readyIndicator.color = isReady ? Color.white : Color.blue;
        if (readyButton != null) readyButton.color = isReady ? new Color(0xBB / 255f, 0xFF / 255f, 0xC1 / 255f) : Color.white;
        if (readyStatusText != null) readyStatusText.text = isReady ? "Wait for the GM to start the game" : "";
       
        Debug.Log($"[Ready] Jogador {(isReady ? "PRONTO " : "NÃO PRONTO ")}.");
    }
    

}
