using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// A registry matching GUIDs to object references for level modifications.
/// </summary>
[CreateAssetMenu(menuName = "Scriptable Objects/Object Registry")]
public class ObjectGuidRegistry : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string objName;
        public string guid;
        public Object obj;
    }

    public List<Entry> entries = new List<Entry>();

    private Dictionary<string, Object> lookup;

    public Object Get(string guid)
    {
        // Build lookup table when adding first object
        if (lookup == null)
        {
            lookup = new Dictionary<string, Object>();
            foreach (Entry entry in entries) if (!lookup.ContainsKey(entry.guid)) lookup[entry.guid] = entry.obj;
        }

        if (guid == null) return null;
        return lookup.TryGetValue(guid, out Object obj) ? obj : null;
    }
}