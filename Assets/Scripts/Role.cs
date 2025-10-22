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
        readyButton.color = Color.white;
        waiting.text = "Press the button when you are ready to start the game";

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

        if (isReady)
        {
            waiting.text = "Waiting for the GM to confirm your data and start the game ";
        }
        else
        {
            waiting.text = "Press the button when you are ready to start the game";
        }
        
       readyButton.color = isReady ? Color.green : Color.white;
       
        Debug.Log($"[Ready] Jogador {(isReady ? "PRONTO " : "NÃO PRONTO ")}.");
    }
    

}
