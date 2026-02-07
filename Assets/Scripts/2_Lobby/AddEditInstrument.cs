using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AddEditInstrument : MonoBehaviour
{
    // add instrument database
    public InstrumentsDatabase instrumentsDatabase;
    //private InstrumentsDatabase instrumentsDatabase;
    [SerializeField] private GameObject instrumentPrefab;
    
    //input fields
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField descriptionInput;

    [SerializeField] private GameObject addInstrumentPanel;
    
    
    private void Awake()
    {
        instrumentsDatabase = InstrumentDatabaseSession.Instance.SessionDb;
        Debug.Log("AddEditInstrument using DB: " + instrumentsDatabase.name);
        
    }
    
    private int GenerateUniqueId()
    {
        int id;
        int safety = 0;

        do
        {
            id = UnityEngine.Random.Range(100, 1000);
            safety++;
        }
        while (instrumentsDatabase.instruments.Exists(i => i.id == id) && safety < 1000);

        if (safety >= 1000)
            Debug.LogError("Failed to generate unique Instrument ID");

        return id;
    }
    
    public void OnClickAddInstrument() // add new instrument to database 
    {
        if (instrumentsDatabase == null)
        {
            Debug.LogError("Instruments Database is not assigned.");
            Debug.LogError($"Instruments Database is not assigned. (from {gameObject.name} / {GetType().Name})");

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
            id = GenerateUniqueId(),
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
        
    
    

