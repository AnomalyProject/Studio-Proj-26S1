///<summary>
///This is Script that is been used like a data container.
///</summary>
using System.Collections.Generic;
using UnityEngine;

[System .Serializable]
public class SaveData
{
    /// <summary>
    /// Hashset of the <see cref="CollectibleSO.ID"/>'s gathered.
    /// </summary>
    public HashSet<string> collectiblesGathered = new();

    /// <summary>
    /// Hashset of <see cref="NarrationEntry.TriggerID"/>'s that have already fired (firstTimeOnly).
    /// </summary>
    public HashSet<string> narratorFiredIDs = new();

    /// <summary>
    /// Dictionary with ID keys and viewed bool values for almanac entries.
    /// </summary>
    public Dictionary<string, bool> almanacEntries = new();
}