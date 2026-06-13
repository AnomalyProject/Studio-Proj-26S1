using UnityEngine;
using System;

[Serializable] public class AnomalyGroup
{
    [SerializeField] private GameObject _groupRoot;
    [SerializeField] private bool _replacesBaseMap;
    [SerializeField, Tooltip("Optional almanac entry.")] private AlmanacEntrySO _almanacEntry;
    public GameObject GroupRoot => _groupRoot;
    public bool ReplacesBaseMap => _replacesBaseMap;
    public AlmanacEntrySO AlmanacEntry => _almanacEntry;

    public AnomalyGroup(GameObject groupRoot, bool replacesBaseMap)
    {
        _groupRoot = groupRoot;
        _replacesBaseMap = replacesBaseMap;
    }
}