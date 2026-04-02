using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class AnomalyMap : MonoBehaviour
{

    [SerializeField, Tooltip("The base map all anomaly variations are tied to.")] GameObject baseMap;
    [SerializeField, Tooltip("The parent objects of anomaly groups.")] List<AnomalyGroup> anomalyVariations;
   
    List<AnomalyGroup> usedAnomalies = new();
    public GameObject BaseMap => baseMap;

    /// <summary>
    /// Returns a random anomaly variation from the list, ensuring that all variations are used before any repeats occur.
    /// </summary>
    /// <returns>The <see cref="GameObject"/> reference of the Anomaly variation.</returns>
    public AnomalyGroup GetNextAnomalyGroup()
    {
        if (anomalyVariations.Count == 0 && usedAnomalies.Count == 0)
        {
            Debug.LogWarning("Anomaly map has no variations assigned.");
            return null;
        }

        if(anomalyVariations.Count == 0)
        {
            anomalyVariations.AddRange(usedAnomalies);
            usedAnomalies.Clear();
        }

        int index = UnityEngine.Random.Range(0, anomalyVariations.Count);
        AnomalyGroup nextAnomaly = anomalyVariations[index];

        usedAnomalies.Add(nextAnomaly);
        anomalyVariations.RemoveAt(index);
        return nextAnomaly;
    }

    /// <summary>
    /// Disables the base map and all anomaly variation GameObjects.
    /// </summary>
    public void DisableAll()
    {
        baseMap?.SetActive(false);

        foreach (var variation in anomalyVariations) variation?.GroupRoot.SetActive(false);
        foreach (var variation in usedAnomalies) variation?.GroupRoot.SetActive(false);
    }
}