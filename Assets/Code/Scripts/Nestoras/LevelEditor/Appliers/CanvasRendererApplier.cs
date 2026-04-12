using static SnapshotUtility;
using UnityEngine.Scripting;
using UnityEngine;
using System;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Applier script to translate set commands by <see cref="ModificationApplier"/> (SerializedProperty path and value) into actual API calls that work in standalone builds.
/// </summary>
[Preserve] // Avoid stripping type from build
public class CanvasRendererApplier : IComponentApplier
{
    public Type TargetType => typeof(CanvasRenderer);
    public bool Supports(string path) => path == "m_CullTransparentMesh";
    public bool Apply(Component target, FieldSnapshot field)
    {
        if (field.path == "m_CullTransparentMesh")
        {
            ((CanvasRenderer)target).cullTransparentMesh = field.GetAs<bool>();
            return true;
        }
        return false;
    }
}