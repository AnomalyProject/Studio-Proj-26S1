using UnityEngine;

public class AlmanacDiscovery : MonoBehaviour
{
    [SerializeField] private AlmanacEntrySO almanacEntry;

    public void DicoverEntry()
    {
        if (RefrenceManager.CurrentSave.almanacEntries.TryAdd(almanacEntry.ID, false))
        {
            SaveSystem.QuickSave(RefrenceManager.CurrentSave);
        }
    }
}