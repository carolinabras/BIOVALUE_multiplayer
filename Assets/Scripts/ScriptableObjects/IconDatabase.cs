using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "IconDatabase", menuName = "Scriptable Objects/IconDatabase")]
public class IconDatabase : ScriptableObject
{
    public List<TypeIconData> icons;

    public Sprite GetIcon(InstrumentType t1, InstrumentType t2)
    {
        foreach (var entry in icons)
        {
            bool sameOrder = entry.type1 == t1 && entry.type2 == t2;
            bool swapped   = entry.type1 == t2 && entry.type2 == t1;

            if (sameOrder || swapped)
                return entry.sprite;
        }
        return null;
    }
}
