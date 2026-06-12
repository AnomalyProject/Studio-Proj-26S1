using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class AlmanacRegistry : MonoBehaviour, IInteractable<PlayerBody>
{
    public struct AlmanacEntryInfo
    {
        public AlmanacEntrySO entryData;
        public bool discovered;
        public bool viewed;

        public AlmanacEntryInfo(AlmanacEntrySO entryData, bool discovered, bool viewed)
        {
            this.entryData = entryData;
            this.discovered = discovered;
            this.viewed = viewed;
        }
    }

    private static readonly HashSet<AlmanacEntrySO> _registry = new();
    public static IReadOnlyCollection<AlmanacEntrySO> Registry => _registry;

    #region Interaction
    public Task<bool> CanInteract(PlayerBody interactor) => Task.FromResult(true);
    public Task<bool> TryInteract(PlayerBody interactor)
    {
        InputBridge.SetContext(InputBridge.InputContext.Almanac);
        return Task.FromResult(true);
    }
    #endregion

    #region Exposed & Control
    public static bool IsEntryDiscovered(string entryID, out bool viewed) => 
        RefrenceManager.CurrentSave.almanacEntries.TryGetValue(entryID, out viewed);
    public static void MarkViewed(string entryID, bool quicksave)
    {
        if(IsEntryDiscovered(entryID, out bool viewed) && !viewed)
        {
            RefrenceManager.CurrentSave.almanacEntries[entryID] = true;

            if(quicksave)
            SaveSystem.QuickSave(RefrenceManager.CurrentSave);
        }
    }
    public static void MarkAllViewed()
    {
        foreach(string discoveredKey in RefrenceManager.CurrentSave.almanacEntries.Keys)
        {
            MarkViewed(discoveredKey, quicksave: false);
        }

        SaveSystem.QuickSave(RefrenceManager.CurrentSave);
    }
    public static IEnumerable<AlmanacEntryInfo> GetAllEntries()
    {
        List<AlmanacEntryInfo> entries = new();

        foreach(AlmanacEntrySO entry in Registry)
        {
            bool discovered = IsEntryDiscovered(entry.ID, out bool viewed);
            AlmanacEntryInfo info = new(entry, discovered, viewed);
            entries.Add(info);
        }

        return entries;
    }
    public static IEnumerable<AlmanacEntryInfo> GetEntriesByCategory(AlmanacType type) =>
    GetAllEntries().Where(e => e.entryData.EntryType == type).OrderBy(e => e.entryData.name);
    public static bool CategoryHasNewEntries(AlmanacType type) =>
    GetAllEntries().Any(e => e.entryData.EntryType == type && e.discovered && !e.viewed);

    /// <summary>
    /// Returns a nomalized float 0-1 of the total completion.
    /// </summary>
    /// <returns></returns>
    public static float GetTotalCompletion()
    {
        float total = Registry.Count;
        if (total == 0) return 1;

        float aquired = RefrenceManager.CurrentSave.almanacEntries.Count;
        return aquired / total;
    }

    /// <summary>
    /// Returns a normalized float 0-1 of a specific almanac type.
    /// </summary>
    /// <param name="entryType"></param>
    /// <returns></returns>
    public static float GetCategoryCompletion(AlmanacType entryType)
    {
        var entries = Registry.Where(entry => entry.EntryType == entryType);

        float total = entries.Count();
        if (total == 0) return 1;

        float aquired = 0;

        foreach(var e in entries)
        {
            if (IsEntryDiscovered(e.ID, out _)) aquired++;
        }

        return aquired / total;
    }
    #endregion

    #region Initialization

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void UpdateRegistry()
    {
        _registry.Clear();
        foreach(var entry in Resources.LoadAll<AlmanacEntrySO>("Almanac")) _registry.Add(entry);
    }
    #endregion
}