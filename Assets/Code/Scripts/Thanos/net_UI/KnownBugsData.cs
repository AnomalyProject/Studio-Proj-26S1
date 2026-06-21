using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BugEntry
{
    public string title;
    [TextArea(1, 3)]
    public string description;
}

[CreateAssetMenu(fileName = "KnownBugsData", menuName = "Config/Known Bugs")]
public class KnownBugsData : ScriptableObject
{
    public List<BugEntry> bugs = new();
}