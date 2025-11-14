using UnityEngine;

public class DetailsHook : MonoBehaviour
{
    [HideInInspector] public Instrument instrument;
    
    [SerializeField] private TMPro.TMP_Text titleText;
    [SerializeField] private TMPro.TMP_Text descriptionText;
    [SerializeField] private TMPro.TMP_Text typeText;
    [SerializeField] private InstrumentHook innerInstrumentHook;
    
    public void SetInstrumentDetails(Instrument inst)
    {
        instrument = inst;
        if (inst == null)
        {
            Debug.LogError("Instrument is null");
            return;
        }
        
        if (titleText)
        {
            titleText.text = instrument.name;
        }
        if (descriptionText)
        {
            descriptionText.text = instrument.generalDescription;
        }
        if (typeText)
        {
            typeText.text = instrument.type.ToString();
        }
        
        if (innerInstrumentHook)
        {
            innerInstrumentHook.SetInstrument(instrument);
        }
    }
    
    public void SetDescription(string description)
    {
        if (descriptionText)
        {
            descriptionText.text = description;
        }

        if (instrument != null)
        {
            instrument.generalDescription = description;
        }
    }
    
    public void DestroySelf()
    {
        Destroy(this.gameObject);
    }
}
