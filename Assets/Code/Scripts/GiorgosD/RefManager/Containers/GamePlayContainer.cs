using System;
using UnityEngine;

/// <summary>
/// helper class to store refs
/// use [SerializedField] private for Inspector assignment 
/// and public read only (=>) for encapsulation and access from other classes 
/// </summary>
[Serializable]
public class GamePlayContainer
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private AnomalyManager _anomalyManager;
    [SerializeField] private MapOrientor _mapOrientor;

    public GameManager GameManager => _gameManager;
    public AnomalyManager AnomalyManager => _anomalyManager;
    public MapOrientor MapOrientor => _mapOrientor;
}
