using static SnapshotUtility;
using UnityEngine;
using System;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Interface for helper scripts that translate SerializedProperty paths and values to actual changes at runtime through Unity's API
/// </summary>
public interface IComponentApplier
{
    public Type TargetType { get; }
    public bool Supports(string path);
    public bool Ignores(string path);
    public bool Apply(Component target, FieldSnapshot field);
}