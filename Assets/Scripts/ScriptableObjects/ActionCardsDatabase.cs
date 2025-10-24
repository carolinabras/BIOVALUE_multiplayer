using UnityEngine;


[CreateAssetMenu(fileName = "ActionCardsDatabase", menuName = "Scriptable Objects/ActionCardsDatabase")]
public class ActionCardsDatabase : ScriptableObject
{
    public ActionCard[] actionCards;
}