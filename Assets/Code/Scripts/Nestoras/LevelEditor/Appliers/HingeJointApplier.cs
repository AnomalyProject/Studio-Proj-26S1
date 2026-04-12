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
public class HingeJointApplier : IComponentApplier
{
    public Type TargetType => typeof(HingeJoint);
    private Dictionary<string, Action<HingeJoint, FieldSnapshot>> supportedFields { get; } = new Dictionary<string, Action<HingeJoint, FieldSnapshot>>()
    {
        { "m_Axis", (hj, field) => hj.axis = field.GetAs<Vector3>() },
        { "m_UseMotor", (hj, field) => hj.useMotor = field.GetAs<bool>() },
        { "m_UseLimits", (hj, field) => hj.useLimits = field.GetAs<bool>() },
        { "m_UseSpring", (hj, field) => hj.useSpring = field.GetAs<bool>() },
        { "m_Spring.spring", (hj, field) => { JointSpring spring = hj.spring; spring.spring = field.GetAs<float>(); hj.spring = spring; } },
        { "m_Spring.damper", (hj, field) => { JointSpring spring = hj.spring; spring.damper = field.GetAs<float>(); hj.spring = spring; } },
        { "m_Spring.targetPosition", (hj, field) => { JointSpring spring = hj.spring; spring.targetPosition = field.GetAs<float>(); hj.spring = spring; } },
        { "m_Motor.targetVelocity", (hj, field) => { JointMotor motor = hj.motor; motor.targetVelocity = field.GetAs<float>(); hj.motor = motor; } },
        { "m_Motor.force", (hj, field) => { JointMotor motor = hj.motor; motor.force = field.GetAs<float>(); hj.motor = motor; } },
        { "m_Motor.freeSpin", (hj, field) => { JointMotor motor = hj.motor; motor.freeSpin = field.GetAs<bool>(); hj.motor = motor; } },
        { "m_UseAcceleration", (hj, field) => hj.useAcceleration = field.GetAs<bool>() },
        { "m_Limits.min", (hj, field) => { JointLimits limits = hj.limits; limits.min = field.GetAs<float>(); hj.limits = limits; } },
        { "m_Limits.max", (hj, field) => { JointLimits limits = hj.limits; limits.max = field.GetAs<float>(); hj.limits = limits; } },
        { "m_Limits.bounciness", (hj, field) => { JointLimits limits = hj.limits; limits.bounciness = field.GetAs<float>(); hj.limits = limits; } },
        { "m_Limits.bounceMinVelocity", (hj, field) => { JointLimits limits = hj.limits; limits.bounceMinVelocity = field.GetAs<float>(); hj.limits = limits; } },
        { "m_Limits.contactDistance", (hj, field) => { JointLimits limits = hj.limits; limits.contactDistance = field.GetAs<float>(); hj.limits = limits; } },
    };
    private HashSet<string> ignoredFields { get; } = new HashSet<string>() { "m_ExtendedLimits" };
    public bool Supports(string path) => supportedFields.ContainsKey(path) || ignoredFields.Contains(path);
    public bool Apply(Component target, FieldSnapshot field)
    {
        if (ignoredFields.Contains(field.path)) return true;
        if (!supportedFields.ContainsKey(field.path)) return false;
        supportedFields[field.path]((HingeJoint)target, field);
        return true;
    }
}