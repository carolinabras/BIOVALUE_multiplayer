using System.Collections.Generic;
using UnityEngine;

public class ActionCardsDatabaseSession : MonoBehaviour
{
    [SerializeField] private ActionCardsDatabase actionCardsDatabase;
    
    public ActionCardsDatabase SessionDb { get; private set; }
    
    public static ActionCardsDatabaseSession Instance;
    
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
        foreach (var actionCard in actionCardsDatabase.actionCards)
        {
                Debug.Log($"ActionCard in SessionDb: {actionCard.descriptionGeneral}, isSelected: {actionCard.isSelected}");
        }
    }

    private void CreateSessionDatabase()
    {
        SessionDb = Instantiate(actionCardsDatabase);
        SessionDb.actionCards = new List<ActionCard>();
        
        foreach (var actionCard in actionCardsDatabase.actionCards)
        {
            SessionDb.actionCards.Add(new ActionCard(actionCard));
        }
        
       
        
        SessionDb.NotifyDatabaseChanged();
    }
}
