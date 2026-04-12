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
public class RigidbodyApplier : IComponentApplier
{
    public Type TargetType => typeof(Rigidbody);
    private Dictionary<string, Action<Rigidbody, FieldSnapshot>> supportedFields { get; } = new Dictionary<string, Action<Rigidbody, FieldSnapshot>>()
    {
        { "m_Mass", (rb, field) => rb.mass = field.GetAs<float>() },
        { "m_LinearDamping", (rb, field) => rb.linearDamping = field.GetAs<float>() },
        { "m_AngularDamping", (rb, field) => rb.angularDamping = field.GetAs<float>() },
        { "m_CenterOfMass", (rb, field) => rb.centerOfMass = field.GetAs<Vector3>() },
        { "m_UseGravity", (rb, field) => rb.useGravity = field.GetAs<bool>() },
        { "m_IsKinematic", (rb, field) => rb.isKinematic = field.GetAs<bool>() },
        { "m_Interpolate", (rb, field) => rb.interpolation = (RigidbodyInterpolation)field.GetAs<int>() },
        { "m_Constraints", (rb, field) => rb.constraints = (RigidbodyConstraints)field.GetAs<int>() },
        { "m_CollisionDetection", (rb, field) => rb.collisionDetectionMode = (CollisionDetectionMode)field.GetAs<int>() },
        { "m_DetectCollisions", (rb, field) => rb.detectCollisions = field.GetAs<bool>() },
        { "m_Velocity", (rb, field) => rb.linearVelocity = field.GetAs<Vector3>() },
        { "m_AngularVelocity", (rb, field) => rb.angularVelocity = field.GetAs<Vector3>() },
        { "m_IncludeLayers", (rb, field) => rb.includeLayers = field.GetAs<int>() },
        { "m_ExcludeLayers", (rb, field) => rb.excludeLayers = field.GetAs<int>() },
        { "m_InertiaTensor", (rb, field) => rb.inertiaTensor = field.GetAs<Vector3>() },
        { "m_InertiaRotation", (rb, field) => rb.inertiaTensorRotation = field.GetAs<Quaternion>() },
    };
    private HashSet<string> ignoredFields { get; } = new HashSet<string>()
    {
        "m_ImplicitCom",
        "m_ImplicitTensor",
    };
    public bool Supports(string path) => supportedFields.ContainsKey(path) || ignoredFields.Contains(path);
    public bool Apply(Component target, FieldSnapshot field)
    {
        if (ignoredFields.Contains(field.path)) return true;
        if (!supportedFields.ContainsKey(field.path)) return false;
        supportedFields[field.path]((Rigidbody)target, field);
        return true;
    }
}