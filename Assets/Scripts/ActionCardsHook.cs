using System;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class ActionCardsHook : MonoBehaviourPun
{
    [HideInInspector] public ActionCard actionCard;

    [SerializeField] private TMPro.TMP_Text description;

    [SerializeField] private TMP_InputField descriptionCustom;

    [SerializeField] private TMP_InputField descriptionHow;
    
    [SerializeField] private TMP_Text descriptionCustomReadOnly; // TMP_Text simples para mostrar o texto
    [SerializeField] private TMP_Text descriptionHowReadOnly;

    public bool isPlayedreal = false;

    public void Start()
    {
        Debug.Log($"ActionCardsHook Start: subscrevendo onActionCardDescriptionChanged");
        if (GameState.Instance != null)
            GameState.Instance.onActionCardDescriptionChanged.AddListener(OnDescriptionChanged);
        else
            Debug.LogWarning("GameState.Instance é null no Start!");
        
    }
    public int ownerPlayerId = -1;
    private void OnDescriptionChanged(int cardId)
    {
        if (actionCard == null || actionCard.id != cardId) return;
        if (!isPlayedreal) return; 
    
        string savedDesc = GameState.Instance?.GetActionCardDescription(ownerPlayerId, actionCard.id) ?? "";
        string savedHow = GameState.Instance?.GetActionCardDescriptionHow(ownerPlayerId, actionCard.id) ?? "";

        if (descriptionCustomReadOnly != null && !string.IsNullOrEmpty(savedDesc))
            descriptionCustomReadOnly.text = savedDesc;
        if (descriptionHowReadOnly != null && !string.IsNullOrEmpty(savedHow))
            descriptionHowReadOnly.text = savedHow;
        
    }

    public void OnCustomValueChanged(string text)
    {
        DescriptionCustom = text;
        Debug.Log("Custom Value Changed: " + text);
    }

    public void OnCustomEndEdit(string text)
    {
        DescriptionCustom = text;
        if (actionCard != null)
            GameState.Instance.SetActionCardDescription(GameState.Instance.localPlayerIndex, actionCard.id, text);
        if (!string.IsNullOrEmpty(text))
            ShowDescriptionCustomReadOnly(text);
    }

    public void OnHowEndEdit(string text)
    {
        DescriptionHow = text;
        if (actionCard != null)
            GameState.Instance.SetActionCardDescriptionHow(GameState.Instance.localPlayerIndex, actionCard.id, text);
        if (!string.IsNullOrEmpty(text))
            ShowDescriptionHowReadOnly(text);
    }

    public void OnHowValueChanged(string text)
    {
        DescriptionHow = text;
    }

    


    public string Description
    {
        get => description != null ? description.text : string.Empty;
        set
        {
            if (actionCard != null)
            {
                actionCard.descriptionGeneral = value;
            }

            if (description != null)
            {
                description.text = value;
            }
        }
    }

    public string DescriptionHow
    {
        get => descriptionHow != null ? descriptionHow.text : string.Empty;
        set
        {
            if (actionCard != null)
                actionCard.descriptionHow = value;

            if (descriptionHow != null && descriptionHow.text != value) 
            {
                descriptionHow.text = value;
                Debug.Log("DescriptionHow set to: " + value);
            }
        }
    }


    public string DescriptionCustom
    {
        get => descriptionCustom != null ? descriptionCustom.text : string.Empty;
        set
        {
            if (actionCard != null)
                actionCard.descriptionGeneral = value;

            if (descriptionCustom != null && descriptionCustom.text != value) // só atualiza se for diferente
                descriptionCustom.text = value;
        }
    }

    public int ID
    {
        get => actionCard != null ? actionCard.id : -1;
        set
        {
            if (actionCard != null)
            {
                actionCard.id = value;
            }
        }
    }

    public bool IsSelected
    {
        get => actionCard != null && actionCard.isSelected;
        set
        {
            if (actionCard != null)
            {
                actionCard.isSelected = value;
            }
        }
    }

    public bool isPlayed
    {
        get => actionCard != null && actionCard.isPlayed;
        set
        {
            if (actionCard != null)
            {
                actionCard.isPlayed = value;
            }
        }
    }

    public void OnSelectedClicked()
    {
        IsSelected = !IsSelected;
        
        // change color of border when selected
    }

    public void SetActionCard(ActionCard card)
    {
        Debug.Log($"SetActionCard: id={card.id}, type={card.type}, desc='{card.descriptionGeneral}', how='{card.descriptionHow}'");
        this.actionCard = card;
        if (card == null) return;

        string savedDesc = GameState.Instance?.GetActionCardDescription(ownerPlayerId, card.id) ?? "";
        string savedHow = GameState.Instance?.GetActionCardDescriptionHow(ownerPlayerId, card.id) ?? "";

        if (isPlayedreal) 
            
        {
            Debug.Log($"isPlayedreal=true, descriptionCustomReadOnly={descriptionCustomReadOnly}, descriptionHowReadOnly={descriptionHowReadOnly}, savedDesc='{savedDesc}', savedHow='{savedHow}', card.desc='{card.descriptionGeneral}', card.how='{card.descriptionHow}'");            if (descriptionCustom != null) descriptionCustom.gameObject.SetActive(false);
            if (descriptionHow != null) descriptionHow.gameObject.SetActive(false);

            if (descriptionCustomReadOnly != null)
            {
                descriptionCustomReadOnly.gameObject.SetActive(true);
                descriptionCustomReadOnly.text = !string.IsNullOrEmpty(savedDesc) ? savedDesc : card.descriptionGeneral;
            }
            if (descriptionHowReadOnly != null)
            {
                descriptionHowReadOnly.gameObject.SetActive(true);
                descriptionHowReadOnly.text = !string.IsNullOrEmpty(savedHow) ? savedHow : card.descriptionHow;
            }
        }
        else
        {
            // Modo edição normal (ActionCardsSpawner)
            Debug.Log($"isPlayedreal=false — entrou no modo edição");

            bool hasCustomType = card.type == ActionCardType.Custom;

            if (hasCustomType && !string.IsNullOrEmpty(savedDesc))
            {
                // Already saved — show as read-only so the text stays visible.
                ShowDescriptionCustomReadOnly(savedDesc);
            }
            else if (descriptionCustom != null)
            {
                descriptionCustom.gameObject.SetActive(hasCustomType);
                if (descriptionCustomReadOnly != null) descriptionCustomReadOnly.gameObject.SetActive(false);
                DescriptionCustom = card.descriptionGeneral;
            }

            if (!string.IsNullOrEmpty(savedHow))
            {
                ShowDescriptionHowReadOnly(savedHow);
            }
            else if (descriptionHow != null)
            {
                if (descriptionHowReadOnly != null) descriptionHowReadOnly.gameObject.SetActive(false);
                DescriptionHow = card.descriptionHow;
            }

            if (card.type == ActionCardType.PreDone)
            {
                Description = card.descriptionGeneral;
            }
        }
    }

    public void SetActionCardInNetwork(ActionCard card)
    {
        if (photonView)
        {
            photonView.RPC(nameof(RPC_SetActionCard), RpcTarget.All, card.id, card.cardName, card.descriptionGeneral,
                card.descriptionHow, card.type);
        }
        else
        {
            Debug.LogWarning("PhotonView is not assigned, setting action card locally.");
            SetActionCard(card);
        }
    }

    [PunRPC]
    public void RPC_SetActionCard(int id, string cardName, string descriptionGeneral, string descriptionHowCard,
        ActionCardType type)
    {
        ActionCard card = new ActionCard
        {
            id = id,
            cardName = cardName,
            descriptionGeneral = descriptionGeneral,
            descriptionHow = descriptionHowCard,
            type = type
        };
        SetActionCard(card);
    }
    
    private void ShowDescriptionCustomReadOnly(string text)
    {
        if (descriptionCustom != null)
            descriptionCustom.gameObject.SetActive(false);
        if (descriptionCustomReadOnly != null)
        {
            descriptionCustomReadOnly.gameObject.SetActive(true);
            descriptionCustomReadOnly.text = text;
        }
    }

    private void ShowDescriptionHowReadOnly(string text)
    {
        if (descriptionHow != null)
            descriptionHow.gameObject.SetActive(false);
        if (descriptionHowReadOnly != null)
        {
            descriptionHowReadOnly.gameObject.SetActive(true);
            descriptionHowReadOnly.text = text;
        }
    }
}