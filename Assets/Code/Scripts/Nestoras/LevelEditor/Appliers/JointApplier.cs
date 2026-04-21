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
public class JointApplier : IComponentApplier
{
    public Type TargetType => typeof(Joint);
    private Dictionary<string, Action<Joint, FieldSnapshot>> supportedFields { get; } = new Dictionary<string, Action<Joint, FieldSnapshot>>()
    {
        { "m_Anchor", (j, field) => j.anchor = field.GetAs<Vector3>() },
        { "m_AutoConfigureConnectedAnchor", (j, field) => j.autoConfigureConnectedAnchor = field.GetAs<bool>() },
        { "m_ConnectedAnchor", (j, field) => j.connectedAnchor = field.GetAs<Vector3>() },
        { "m_BreakForce", (j, field) => j.breakForce = field.GetAs<float>() },
        { "m_BreakTorque", (j, field) => j.breakTorque = field.GetAs<float>() },
        { "m_EnableCollision", (j, field) => j.enableCollision = field.GetAs<bool>() },
        { "m_EnablePreprocessing", (j, field) => j.enablePreprocessing = field.GetAs<bool>() },
        { "m_MassScale", (j, field) => j.massScale = field.GetAs<float>() },
        { "m_ConnectedMassScale", (j, field) => j.connectedMassScale = field.GetAs<float>() },
    };
    public bool Supports(string path) => supportedFields.ContainsKey(path);
    public bool Ignores(string path) => false;
    public bool Apply(Component target, FieldSnapshot field)
    {
        if (!supportedFields.ContainsKey(field.path)) return false;
        supportedFields[field.path]((Joint)target, field);
        return true;
    }
}