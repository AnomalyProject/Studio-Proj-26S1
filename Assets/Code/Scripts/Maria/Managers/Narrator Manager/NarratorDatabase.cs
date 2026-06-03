using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Narrator/Database", fileName = "NarratorDatabase")]
public class NarratorDatabase : ScriptableObject
{
    [SerializeField] private List<NarratorEntry> entries = new();

    public NarratorEntry GetEntry(string triggerID)
    {
        return entries.Find(e => e.triggerID == triggerID);
    }
    private void OnValidate()
    {
        var seen = new HashSet<string>();
        foreach (var entry in entries)
        {
            if (string.IsNullOrEmpty(entry.triggerID)) continue;
            if (!seen.Add(entry.triggerID))
                Debug.LogWarning($"[NarratorDatabase] Duplicate triggerID found: '{entry.triggerID}'", this);
        }
    }
}