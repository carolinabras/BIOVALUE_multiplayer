using System.Collections.Generic;
using UnityEngine;

public class GameKnowledge : MonoBehaviour
{
    #region Singleton

    private static GameObject _instanceGameObject;
    private static GameKnowledge _instance;

    public static GameKnowledge Instance
    {
        get
        {
            if (_instance) return _instance;
            if (_instanceGameObject)
            {
                _instance = _instanceGameObject.GetComponent<GameKnowledge>();
                if (_instance) return _instance;
            }

            GameObject prefab = Resources.Load("GameKnowledge") as GameObject;
            if (!prefab)
            {
                Debug.LogError("No GameKnowledge prefab found in Resources folder.");
                return null;
            }

            _instanceGameObject = Instantiate(prefab);
            DontDestroyOnLoad(_instanceGameObject);
            if (!_instanceGameObject)
            {
                Debug.LogError("Failed to instantiate GameKnowledge prefab.");
                return null;
            }

            _instance = _instanceGameObject.GetComponent<GameKnowledge>();
            if (!_instance)
            {
                Debug.LogError("GameKnowledge component not found on the instantiated prefab.");
                return null;
            }

            return _instance;
        }
    }

    #endregion

    public InstrumentsDatabase instrumentsDatabase;
    public ActionCardsDatabase actionCardsDatabase;
    
    public List<int> playedInstrumentIds = new List<int>();
}