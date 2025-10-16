using UnityEngine;

public class InstrumentHook : MonoBehaviour
{
    [HideInInspector] public Instrument instrument;

    [SerializeField] private TMPro.TMP_Text nameText;
    
    [SerializeField] private DragPiece dragPiece;

    public string Name
    {
        get => nameText != null ? nameText.text : string.Empty;
        set
        {
            if (nameText != null)
            {
                nameText.text = value;
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
}