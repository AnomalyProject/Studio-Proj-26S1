using System;
using UnityEngine;

[Serializable]
public class AnomalyMap
{
    [SerializeField, Tooltip("The base map all anomaly variations are tied to.")] GameObject baseMap;
    [SerializeField, Tooltip("The parent objects of anomaly groups.")] GameObject[] anomalyVariations;
    public GameObject BaseMap => baseMap;
    public GameObject[] AnomalyVariations => anomalyVariations;
}
