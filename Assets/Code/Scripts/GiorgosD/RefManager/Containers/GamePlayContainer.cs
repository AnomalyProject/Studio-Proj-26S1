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
    [SerializeField] private TestRef testRef;
    public TestRef TestRef => testRef;
}
