using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "ObjectivesDatabase", menuName = "Scriptable Objects/Objectives Database")]
public class ObjectivesDatabase : ScriptableObject
{
    public List<Objective> objectives;

    public Objective GetObjectiveById(int id)
    {
        return objectives.Find(objective => objective.id == id);
    }
}