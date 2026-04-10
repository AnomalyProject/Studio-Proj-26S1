using System.Collections.Generic;
using static SnapshotUtility;
using UnityEngine;

[CreateAssetMenu(menuName = "Anomaly/Level Modification")]
public class LevelModification : ScriptableObject
{
    public List<GameObjectSnapshot> added = new List<GameObjectSnapshot>();
    public List<GameObjectSnapshot> removed = new List<GameObjectSnapshot>();

    public List<GameObjectModification> modified = new List<GameObjectModification>();
}
