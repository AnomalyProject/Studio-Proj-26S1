using UnityEngine;

public class AlmanacDiscovery : MonoBehaviour
{
    [SerializeField] private AlmanacEntrySO almanacEntry;

    public void DicoverEntry() => Discover(almanacEntry);
    public static void Discover(AlmanacEntrySO entry)
    {
        if (RefrenceManager.CurrentSave.almanacEntries.TryAdd(entry.ID, false))
        {
            SaveSystem.QuickSave(RefrenceManager.CurrentSave);
        }
    }
}