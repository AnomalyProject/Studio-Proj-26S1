using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AnomalyMap
{
    [SerializeField] bool wholeRoomVariations = true;
    [SerializeField, Tooltip("The base map all anomaly variations are tied to.")] GameObject baseMap;
    [SerializeField, Tooltip("The parent objects of anomaly groups.")] List<GameObject> anomalyVariations;
   
    List<GameObject> usedAnomalies = new();
    public bool WholeRoomVariations => wholeRoomVariations;
    public GameObject BaseMap => baseMap;

    /// <summary>
    /// Returns a random anomaly variation from the list, ensuring that all variations are used before any repeats occur.
    /// </summary>
    /// <returns>The <see cref="GameObject"/> reference of the Anomaly variation.</returns>
    public GameObject GetNextAnomalyGroup()
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
        GameObject nextAnomaly = anomalyVariations[index];

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

        foreach (var variation in anomalyVariations) variation?.SetActive(false);
        foreach (var variation in usedAnomalies) variation?.SetActive(false);
    }
}
