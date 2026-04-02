using System;
using UnityEngine;

[Serializable] public class AnomalyGroup
{
    [SerializeField] GameObject _groupRoot;
    [SerializeField] bool _replacesBaseMap;
    public GameObject GroupRoot => _groupRoot;
    public bool ReplacesBaseMap => _replacesBaseMap;
}