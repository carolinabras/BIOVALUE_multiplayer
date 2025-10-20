using System;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class InstrumentHook : MonoBehaviour
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

    public void SetInstrument(Instrument instr)
    {
        this.instrument = instr;
        if (instr == null)
        {
            Debug.LogError("Instrument is null");
            return;
        }

        Name = instr.name;
    }

    [PunRPC]
    public void SetInstrumentRPC(int instrumentId)
    {
        var instrumentData = GameKnowledge.Instance?.instrumentsDatabase?.GetInstrumentById(instrumentId);
        if (instrumentData == null)
        {
            Debug.LogError($"Instrument with ID {instrumentId} not found in database.");
            return;
        }
        SetInstrument(instrumentData);
    }
}