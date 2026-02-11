using System;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InstrumentHook : MonoBehaviourPun
{
    [HideInInspector] public Instrument instrument;

    [SerializeField] private TMP_Text nameText;

    [SerializeField] private TMP_Text typeOne;
    [SerializeField] private TMP_Text typeTwo;

    [SerializeField] private TMP_Text id;

    [SerializeField] private DragPiece dragPiece;

    [SerializeField] private Image iconImage;
    [SerializeField] private IconDatabase iconDatabase;
    

    public string Name
    {
        get => instrument != null ? instrument.name : string.Empty;
        set
        {
            if (instrument != null)
            {
                instrument.name = value;
            }

            if (nameText)
            {
                nameText.text = value;
            }
        }
    }
    
    public string TypeOne
    {
        get => instrument != null ? instrument.typeOne.ToString() : string.Empty;
        set
        {
            if (instrument != null && Enum.TryParse(value, out InstrumentType type))
            {
                instrument.typeOne = type;
            }

            if (typeOne)
            {
                typeOne.text = value;
            }
        }
    }
    
    public string TypeTwo
    {
        get => instrument != null ? instrument.typeTwo.ToString() : string.Empty;
        set
        {
            if (instrument != null && Enum.TryParse(value, out InstrumentType type))
            {
                instrument.typeTwo = type;
            }

            if (typeTwo)
            {
                typeTwo.text = value;
            }
        }
    }
    
    public string Id
    {
        get => instrument != null ? instrument.id.ToString() : string.Empty;
        set
        {
            if (instrument != null && int.TryParse(value, out int idValue))
            {
                instrument.id = idValue;
            }

            if (id)
            {
                id.text = value;
            }
        }
    }
    
    public Sprite Icon
    {
        get => instrument != null ? instrument.icon : null;
        set
        {
            if (instrument == null) return;

            instrument.icon = value;

            if (iconImage != null)
            {
                iconImage.sprite = value;
                iconImage.enabled = (value != null);
            }
        }
    }

    
    
    public string GeneralDescription
    {
        get => instrument != null ? instrument.generalDescription : string.Empty;
        set
        {
            if (instrument != null)
            {
                instrument.generalDescription = value;
            }
        }
    }

    [SerializeField] private TMP_Text descriptionText;

    public string Description
    {
        get => instrument != null ? instrument.description : string.Empty;
        set
        {
            if (instrument != null)
            {
                instrument.description = value;
            }

            if (descriptionText)
            {
                descriptionText.text = value;
            }
        }
    }
    
    public bool isSelected
    {
        get => instrument != null && instrument.isSelected;
        set
        {
            if (instrument != null)
            {
                instrument.isSelected = value;
            }
        }
    }

    public void SetInstrument(Instrument instr)
    {
        this.instrument = instr;
        if (instr == null)
        {
            Debug.LogError("Instrument is null");
            return;
        }

        Name = instr.name;
        Description = instr.description;
        
        if ( instr.typeOne != InstrumentType.None ){
            TypeOne = instr.typeOne.ToString();
            
        }
        else
        {
            TypeOne = "";
        }

        if (instr.typeTwo != InstrumentType.None)
        {
            TypeTwo = instr.typeTwo.ToString();
        }
        else
        {
            TypeTwo = "";
        }

        if (instr.id <= 100)
        {
            Id = instr.id.ToString();
        }
        else        {
            Id = "";
        }
        
        
    
        var sprite = iconDatabase.GetIcon(instr.typeOne, instr.typeTwo);

        
        instrument.icon = sprite;

       
        if (iconImage != null)
        {
            iconImage.sprite = sprite;
            iconImage.enabled = (sprite != null);
        }
        else {
           
        }
        
        /* var sprite = iconDatabase.GetIcon(instr.typeOne, instr.typeTwo);
        if (iconImage != null)
        {
            iconImage.sprite = sprite;
            iconImage.enabled = (sprite != null);
        } */
        
        Debug.Log(
            $"[InstrumentHook] {instr.name} types=({instr.typeOne},{instr.typeTwo}) " +
            $"iconDb={(iconDatabase ? iconDatabase.name : "NULL")} " +
            $"iconImage={(iconImage ? iconImage.name : "NULL")} " +
            $"sprite={(sprite ? sprite.name : "NULL")}"
        );
        
    }

    public void SetInstrumentInNetwork(Instrument instr)
    {
        if (photonView)
        {
            photonView.RPC(nameof(RPC_SetInstrument), RpcTarget.All, instr.id, instr.name, instr.description,
                instr.typeOne, instr.typeTwo);
        }
        else
        {
            Debug.LogWarning("PhotonView is not assigned, setting instrument locally.");
            SetInstrument(instr);
        }
    }

    [PunRPC]
    public void RPC_SetInstrument(int id, string instrumentName, string description, InstrumentType type1, InstrumentType type2)
    {
        Instrument instr = new Instrument
        {
            id = id,
            name = instrumentName,
            description = description,
            typeOne = type1,
            typeTwo = type2
            
        };
        SetInstrument(instr);
    }
}