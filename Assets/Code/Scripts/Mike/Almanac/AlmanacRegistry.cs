using System;
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
    /// <summary>
    /// Does the entry exist in the current save file?
    /// </summary>
    /// <returns>True if the entry exists.</returns>
    public static bool IsEntryDiscovered(string entryID, out bool viewed) => 
        RefrenceManager.CurrentSave.almanacEntries.TryGetValue(entryID, out viewed);

    /// <inheritdoc cref="IsEntryDiscovered(string, out bool)"/>
    public static bool IsEntryDiscovered(AlmanacEntrySO entry, out bool viewed) => IsEntryDiscovered(entry.ID, out viewed);

    /// <summary>
    /// Marks an entry as viewed if discovered.
    /// </summary>
    /// <param name="entryID"></param>
    /// <param name="quicksave"></param>
    public static void MarkViewed(string entryID, bool quicksave)
    {
        if(IsEntryDiscovered(entryID, out bool viewed) && !viewed)
        {
            RefrenceManager.CurrentSave.almanacEntries[entryID] = true;

            if(quicksave)
            SaveSystem.QuickSave(RefrenceManager.CurrentSave);
        }
    }

    /// <inheritdoc cref="MarkViewed(string, bool)"/>
    public static void MarkViewed(AlmanacEntrySO entry, bool quicksave) => MarkViewed(entry.ID, quicksave);

    /// <summary>
    /// Marks all discovered entries as viewed.
    /// </summary>
    public static void MarkAllViewed()
    {
        var save = RefrenceManager.CurrentSave;
        var entryKeys = save.almanacEntries.Keys.ToList();

        foreach (string key in entryKeys) MarkViewed(key, quicksave: false);

        SaveSystem.QuickSave(save);
    }

    /// <summary>
    /// Every entry in the registry, disovered or not.
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Every entry in a category. Discovered or not.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IEnumerable<AlmanacEntryInfo> GetEntriesByCategory(AlmanacType type) =>
    GetAllEntries().Where(e => e.entryData.EntryType == type).OrderBy(e => e.entryData.name);

    /// <summary>
    /// Wheather a category has any entry that is not marked as viewed.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
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

    public static void DebugAlmanac()
    {
        string output = "[ALMANAC]\n\n";

        output += $"Registry Count: {Registry.Count}\n";
        output += $"Total Progress: ({GetTotalCompletion()}/1)\n";
        output += "\n";
        foreach(var type in Enum.GetValues(typeof(AlmanacType)))
        {
            output += $"[{type.ToString()}] Progress: ({GetCategoryCompletion((AlmanacType)type)}/1) ---\n";

            IEnumerable<AlmanacEntryInfo> typeEntries = GetEntriesByCategory((AlmanacType)type);
            foreach(var entry in typeEntries)
            {
                output += $"{entry.entryData.CollectibleName} | Discovered: {entry.discovered} | Viewed: {entry.viewed}\n";
            }
            output += "\n";
        }
        Debug.Log(output);
    }
}