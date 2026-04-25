using System.Collections.Generic;
using static SnapshotUtility;
using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Object that holds all data needed to apply and revert a modification to the level.
/// </summary>
[CreateAssetMenu(menuName = "Scriptable Objects/Level Modification")]
public class LevelModification : ScriptableObject
{
    public List<GameObjectSnapshot> addedGameObjects = new List<GameObjectSnapshot>();
    public List<GameObjectSnapshot> removedGameObjects = new List<GameObjectSnapshot>();

    public List<GameObjectModification> gameObjectModifications = new List<GameObjectModification>();
}
