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
    public HashSet<string> supportedFields { get; } = new HashSet<string>()
    {
        "m_Mesh",
        "m_SharedMesh",
    };

    public bool Supports(string path) => supportedFields.Contains(path);

    public bool Apply(Component target, FieldSnapshot field)
    {
        MeshFilter f = (MeshFilter)target;

        switch (field.path)
        {
            case "m_Mesh":
            case "m_SharedMesh":
                f.sharedMesh = field.GetAsObject() as Mesh;
                return true;
        }

        return false;
    }
}