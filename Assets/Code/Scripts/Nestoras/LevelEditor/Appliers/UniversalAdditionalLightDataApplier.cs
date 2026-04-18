using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting;
using UnityEngine;
using static SnapshotUtility;
using System;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Applier script to translate set commands by <see cref="ModificationApplier"/> (SerializedProperty path and value) into actual API calls that work in standalone builds.
/// </summary>
[Preserve] // Avoid stripping type from build
public class UniversalAdditionalLightDataApplier : IComponentApplier
{
    public Type TargetType => typeof(UniversalAdditionalLightData);
    public bool Supports(string path) => false;
    public bool Ignores(string path) => true;
    public bool Apply(Component target, FieldSnapshot field) => false;
}