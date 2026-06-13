using UnityEngine;
using System;

[Serializable] public class AnomalyGroup
{
    [SerializeField] GameObject _groupRoot;
    [SerializeField] bool _replacesBaseMap;
    public GameObject GroupRoot => _groupRoot;
    public bool ReplacesBaseMap => _replacesBaseMap;

    public AnomalyGroup(GameObject groupRoot, bool replacesBaseMap)
    {
        _groupRoot = groupRoot;
        _replacesBaseMap = replacesBaseMap;
    }
}