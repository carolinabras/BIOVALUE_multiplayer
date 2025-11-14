using System;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class InstrumentHook : MonoBehaviourPun
{
    [HideInInspector] public Instrument instrument;

    [SerializeField] private TMP_Text nameText;

    [SerializeField] private DragPiece dragPiece;

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
    
    public Sprite Icon
    {
        get => instrument != null ? instrument.icon : null;
        set
        {
            if (dragPiece != null)
            {
                instrument.icon = value;
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
        Icon = instr.icon;
        
    }

    public void SetInstrumentInNetwork(Instrument instr)
    {
        if (photonView)
        {
            photonView.RPC(nameof(RPC_SetInstrument), RpcTarget.All, instr.id, instr.name, instr.description,
                instr.type);
        }
        else
        {
            Debug.LogWarning("PhotonView is not assigned, setting instrument locally.");
            SetInstrument(instr);
        }
    }

    [PunRPC]
    public void RPC_SetInstrument(int id, string instrumentName, string description, InstrumentType type)
    {
        Instrument instr = new Instrument
        {
            id = id,
            name = instrumentName,
            description = description,
            type = type
        };
        SetInstrument(instr);
    }
}