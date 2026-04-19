using static SnapshotUtility;
using UnityEngine.Scripting;
using UnityEngine.UI;
using UnityEngine;
using System;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Applier script to translate set commands by <see cref="ModificationApplier"/> (SerializedProperty path and value) into actual API calls that work in standalone builds.
/// </summary>
[Preserve] // Avoid stripping type from build
public class GraphicRaycasterApplier : IComponentApplier
{
    public Type TargetType => typeof(GraphicRaycaster);
    public bool Supports(string path) => path == "m_BlockingMask";
    public bool Ignores(string path) => false;
    public bool Apply(Component target, FieldSnapshot field)
    {
        if (field.path != "m_BlockingMask") return false;
        ((GraphicRaycaster)target).blockingObjects = (GraphicRaycaster.BlockingObjects)field.GetAs<int>();
        return true;
    }
}