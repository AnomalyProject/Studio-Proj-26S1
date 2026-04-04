using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class AnomalyMap : GameMap
{
    [SerializeField, Tooltip("The parent objects of anomaly groups.")] List<AnomalyGroup> anomalyVariations;
   
    List<AnomalyGroup> usedAnomalies = new(), availableAnomalies = new();

    protected override void Awake()
    {
        base.Awake();
        availableAnomalies.AddRange(anomalyVariations);
    }

    /// <summary>
    /// Returns a random anomaly variation from the list, ensuring that all variations are used before any repeats occur.
    /// </summary>
    /// <returns>The <see cref="GameObject"/> reference of the Anomaly variation.</returns>
    public AnomalyGroup GetNextAnomalyGroup()
    {
        if (anomalyVariations.Count == 0)
        {
            Debug.LogWarning($"{name}: Anomaly map has no variations assigned.");
            return null;
        }

        if(availableAnomalies.Count == 0)
        {
            availableAnomalies.AddRange(usedAnomalies);
            usedAnomalies.Clear();
        }

        int index = UnityEngine.Random.Range(0, availableAnomalies.Count);
        AnomalyGroup nextAnomaly = availableAnomalies[index];

        usedAnomalies.Add(nextAnomaly);
        availableAnomalies.RemoveAt(index);
        return nextAnomaly;
    }

    /// <summary>
    /// Disables the base map and all anomaly variation GameObjects.
    /// </summary>
    public void DisableAll(bool keepBase = false)
    {
        BaseMap?.SetActive(keepBase);

        foreach (var variation in availableAnomalies) variation?.GroupRoot.SetActive(false);
        foreach (var variation in usedAnomalies) variation?.GroupRoot.SetActive(false);
    }
}