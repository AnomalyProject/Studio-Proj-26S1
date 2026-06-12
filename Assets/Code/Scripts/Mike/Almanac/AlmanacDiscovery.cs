using UnityEngine;

public class AlmanacDiscovery : MonoBehaviour
{
    [SerializeField] private AlmanacEntrySO almanacEntry;

    /// <inheritdoc cref="Discover(AlmanacEntrySO)"/>
    public void DiscoverEntry() => Discover(almanacEntry);

    /// <summary>
    /// Discover and register the specified entry in the current save.
    /// </summary>
    /// <param name="entry"></param>
    public static void Discover(AlmanacEntrySO entry)
    {
        if (entry == null) return;

        if (RefrenceManager.CurrentSave.almanacEntries.TryAdd(entry.ID, false))
        {
            SaveSystem.QuickSave(RefrenceManager.CurrentSave);
        }
    }
}