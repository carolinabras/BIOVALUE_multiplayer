using UnityEngine;

public class DetailsHook : MonoBehaviour
{
    [HideInInspector] public Instrument instrument;
    
    [SerializeField] private TMPro.TMP_Text titleText;
    [SerializeField] private TMPro.TMP_Text descriptionText;
    [SerializeField] private TMPro.TMP_Text typeTextOne;
    [SerializeField] private TMPro.TMP_Text typeTextTwo;
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
        if (typeTextOne)
        {
            typeTextOne.text = instrument.typeOne.ToString();
        }
        if (typeTextOne)
        {
            typeTextOne.text = instrument.typeOne.ToString();
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
