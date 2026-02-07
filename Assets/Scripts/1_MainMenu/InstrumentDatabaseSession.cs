using System;
using System.Collections.Generic;
using UnityEngine;

public class InstrumentDatabaseSession : MonoBehaviour
{
    [SerializeField] private InstrumentsDatabase masterDatabase;
    public InstrumentsDatabase SessionDb { get; private set; }

    public static InstrumentDatabaseSession Instance;
    
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateSessionDatabase();
    }

    private void CreateSessionDatabase()
    {
        SessionDb = Instantiate(masterDatabase);
        SessionDb.instruments = new List<Instrument>();

        foreach (var inst in masterDatabase.instruments)
        {
            SessionDb.instruments.Add(new Instrument
            {
                id = inst.id,
                name = inst.name,
                description = inst.description,
                generalDescription = inst.generalDescription,
                isSelected = inst.isSelected,
                typeOne = inst.typeOne,
                typeTwo = inst.typeTwo,
                
                icon = inst.icon
                
                
            });
        }

        SessionDb.NotifyDatabaseChanged();
    }
}


