using static SnapshotUtility;
using UnityEngine;
using System;

public interface IComponentApplier
{
    public Type TargetType { get; }
    public bool Apply(Component target, FieldSnapshot field);
}