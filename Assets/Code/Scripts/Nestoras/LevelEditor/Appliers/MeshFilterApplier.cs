using System.Collections.Generic;
using System;
using static SnapshotUtility;
using UnityEngine.Scripting;
using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Applier script to translate set commands by <see cref="ModificationApplier"/> (SerializedProperty path and value) into actual API calls that work in standalone builds.
/// </summary>
[Preserve] // Avoid stripping type from build
public class MeshFilterApplier : IComponentApplier
{
    public Type TargetType => typeof(MeshFilter);
    private Dictionary<string, Action<MeshFilter, FieldSnapshot>> supportedFields { get; } = new Dictionary<string, Action<MeshFilter, FieldSnapshot>>()
    {
        { "m_Mesh", (f, field) => f.sharedMesh = field.GetAsObject() as Mesh },
        { "m_SharedMesh", (f, field) => f.sharedMesh = field.GetAsObject() as Mesh },
    };
    public bool Supports(string path) => supportedFields.ContainsKey(path);
    public bool Ignores(string path) => false;
    public bool Apply(Component target, FieldSnapshot field)
    {
        if (!supportedFields.ContainsKey(field.path)) return false;
        supportedFields[field.path]((MeshFilter)target, field);
        return true;
    }
}