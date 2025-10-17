using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionCardsHook : MonoBehaviour
{
    [HideInInspector] public ActionCard actionCard;

    [SerializeField] private TMPro.TMP_Text description;

    [SerializeField] private TMP_InputField descriptionCustom; 
    
    [SerializeField] private TMP_InputField  descriptionHow;

    [SerializeField] private int id;
    
    
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
        get => id;
        set => id = value;
    }
    
    public void SetActionCard (ActionCard card)
    {
        this.actionCard = card;
        if (card == null)
        {
            Debug.LogError("Action Card is null");
            return;
        }
        
        ID = card.id;
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
