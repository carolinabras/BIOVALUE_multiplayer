using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionCardsHook : MonoBehaviour
{
    [HideInInspector] public ActionCard actionCard;

    [SerializeField] private TMPro.TMP_Text description;

    [SerializeField] private TMP_InputField descriptionCustom; 
    
    [SerializeField] private TMP_InputField  descriptionHow;
    
    
    public void OnCustomValueChanged(string text)   { DescriptionCustom = text; Debug.Log("Custom Value Changed: " + text); }
    public void OnCustomEndEdit(string text)        { DescriptionCustom = text; Debug.Log("Description changed" + text);   }

    public void OnHowValueChanged(string text)      { DescriptionHow = text;   }
    public void OnHowEndEdit(string text)           { DescriptionHow = text;   Debug.Log("Description how changed" + text);   }

   
    
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
            {
                actionCard.descriptionHow = value;
            }  
          
            if (descriptionHow != null)
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

            if (descriptionCustom != null)
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
    }
    
    public void SetActionCard (ActionCard card)
    {
        this.actionCard = card;
        if (card == null)
        {
            Debug.LogError("Action Card is null");
            return;
        }
        
        DescriptionHow = card.descriptionHow;
        
        if (card.type == ActionCardType.Custom)
        {
            if (descriptionCustom != null)
            {
                descriptionCustom.gameObject.SetActive(true);
                DescriptionCustom = card.descriptionGeneral;
            }
            
        }
        if (card.type == ActionCardType.PreDone)
        {
            if (descriptionCustom != null)
            {
                descriptionCustom.gameObject.SetActive(false);
                Description = card.descriptionGeneral;
            }
        }
    }

}
