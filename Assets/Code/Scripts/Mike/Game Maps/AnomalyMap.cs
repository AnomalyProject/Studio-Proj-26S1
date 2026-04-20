using System.Collections.Generic;
using UnityEngine;

public class AnomalyMap : GameMap
{
    [SerializeField, Tooltip("The normal version of the map, no anomalies.")] private GameObject baseMap;
    [SerializeField, Tooltip("The parent objects of anomaly groups.")] private List<AnomalyGroup> anomalyVariations;
   
    private List<int> availableIndices = new();
    public GameObject BaseMap => baseMap;

    protected override void Awake()
    {
        base.Awake();
        anomalyVariations.RemoveAll(group => group.GroupRoot == null);
        ResetAvailableIndices();
    }

    /// <summary>
    /// Adds new <see cref="AnomalyGroup"/> to <see cref="anomalyVariations"/>.
    /// </summary>
    /// <param name="newGroup">The group to add.</param>
    public void AddVariation(AnomalyGroup newGroup)
    {
        anomalyVariations.Add(newGroup);
        anomalyVariations.RemoveAll(group => group.GroupRoot == null);
    }

    /// <summary>
    /// Returns a random anomaly variation from the list, ensuring that all variations are used before any repeats occur.
    /// </summary>
    /// <returns>The <see cref="GameObject"/> reference of the Anomaly variation.</returns>
    public AnomalyGroup GetNextAnomalyGroup()
    {
        int index = GetRandomUnusedAnomalyIndex();
        return GetAnomalyGroupAtIndex(index);
    }
    public AnomalyGroup GetAnomalyGroupAtIndex(int index)
    {
        if (index < 0 || index >= anomalyVariations.Count) return null;
        return anomalyVariations[index];
    }

    public int GetRandomUnusedAnomalyIndex()
    {
        if (anomalyVariations.Count == 0)
        {
            Debug.LogWarning($"{name}: No variations.");
            return -1;
        }

        if (availableIndices.Count == 0) ResetAvailableIndices();

        int rand = UnityEngine.Random.Range(0, availableIndices.Count);
        int index = availableIndices[rand];

        availableIndices.RemoveAt(rand);

        return index;
    }
    public void ResetAvailableIndices()
    {
        availableIndices.Clear();
        for (int i = 0; i < anomalyVariations.Count; i++) availableIndices.Add(i);
    }

    /// <summary>
    /// Disables the base map and all anomaly variation GameObjects.
    /// </summary>
    public void DisableAll(bool keepBase = false)
    {
        BaseMap?.SetActive(keepBase);

        foreach (var variation in anomalyVariations)
        {
            variation.GroupRoot?.SetActive(false);
        }
    }
}