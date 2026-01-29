using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AddEditInstrument : MonoBehaviour
{
    // add instrument database
    public InstrumentsDatabase instrumentsDatabase;
    
    [SerializeField] private GameObject instrumentPrefab;
    
    //input fields
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField descriptionInput;

    [SerializeField] private GameObject addInstrumentPanel;
    
    public void OnClickAddInstrument() // add new instrument to database 
    {
        if (instrumentsDatabase == null)
        {
            Debug.LogError("Instruments Database is not assigned.");
            return;
        }
        
        string novoNome = nameInput.text;
        string descriptionNew = descriptionInput.text;
        
        if (string.IsNullOrEmpty(novoNome))
        {
            Debug.LogError("Instrument name cannot be empty.");
            return;
        }
        
        // create new instrument
        Instrument newInstrument = new Instrument
        {
            name = novoNome,
            description = descriptionNew,
            generalDescription = descriptionNew,
            
        };
        
        // add to database
        instrumentsDatabase.AddInstrument(newInstrument);
        
        
        Debug.Log($"Added new instrument: {novoNome}");
        
        // clear input fields
        nameInput.text = "";
        descriptionInput.text = "";
        
    }
    
    public void OpenAddInstrumentPanel()
    {
        if (addInstrumentPanel == null) return;

        LeanTween.cancel(addInstrumentPanel);

        addInstrumentPanel.SetActive(true);
        addInstrumentPanel.transform.localScale = Vector3.zero;

        LeanTween.scale(addInstrumentPanel, Vector3.one, 0.25f)
            .setEaseOutBack()
            .setIgnoreTimeScale(true);
    }
    
    public void CloseAddInstrumentPanel()
    {
        if (addInstrumentPanel == null) return;

        LeanTween.cancel(addInstrumentPanel);

        LeanTween.scale(addInstrumentPanel, Vector3.zero, 0.2f)
            .setEaseInBack()
            .setIgnoreTimeScale(true)
            .setOnComplete(() =>
            {
                addInstrumentPanel.SetActive(false);
            });
    }
    
    public void DeleteInstruments()
    {
        if (instrumentsDatabase == null)
        {
            Debug.LogError("Instruments Database is not assigned.");
            return;
        }

        instrumentsDatabase.RemoveSelectedInstruments();
        Debug.Log("Deleted all selected instruments.");
    }
        
}
        
    
    

