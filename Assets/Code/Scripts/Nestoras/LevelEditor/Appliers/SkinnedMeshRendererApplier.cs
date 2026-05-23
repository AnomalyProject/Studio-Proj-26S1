using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Scripting;
using static SnapshotUtility;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Applier script to translate set commands by <see cref="ModificationApplier"/> (SerializedProperty path and value) into actual API calls that work in standalone builds.
/// </summary>
[Preserve] // Avoid stripping type from build
public class SkinnedMeshRendererApplier : IComponentApplier
{
    public Type TargetType => typeof(SkinnedMeshRenderer);
    private Dictionary<string, Action<SkinnedMeshRenderer, FieldSnapshot>> supportedFields { get; } = new Dictionary<string, Action<SkinnedMeshRenderer, FieldSnapshot>>()
    {
        { "m_Quality", (r, field) => r.quality = field.GetAs<SkinQuality>() },
        { "m_UpdateWhenOffscreen", (r, field) => r.updateWhenOffscreen = field.GetAs<bool>() },
        { "m_SkinnedMotionVectors", (r, field) => r.skinnedMotionVectors = field.GetAs<bool>() },
        { "m_Mesh", (r, field) => r.sharedMesh = field.GetAs<Mesh>() },
        { "m_RootBone", (r, field) => r.rootBone = field.GetAs<Transform>() },
        { "m_AABB", (r, field) => r.localBounds = field.GetAs<Bounds>() },
    };
    private HashSet<string> ignoredFields { get; } = new HashSet<string>()
    {
        "m_ConstrainProportionsScale",
        "m_BlendShapeWeights.Array.size"
    };
    public bool Supports(string path) => supportedFields.ContainsKey(path) || path.StartsWith("m_BlendShapeWeights.Array.data[");
    public bool Ignores(string path) => ignoredFields.Contains(path);
    public bool Apply(Component target, FieldSnapshot field)
    {
        SkinnedMeshRenderer r = (SkinnedMeshRenderer)target;

        // Haven't tested this. Could break if the size of the list changes. Oh well...
        if (field.path.StartsWith("m_BlendShapeWeights.Array.data["))
        {
            int start = field.path.IndexOf('[') + 1;
            int end = field.path.IndexOf(']');
            int index = int.Parse(field.path.Substring(start, end - start));

            float weight = field.GetAs<float>();
            r.SetBlendShapeWeight(index, weight);

            return true;
        }

        if (!supportedFields.ContainsKey(field.path)) return false;
        supportedFields[field.path](r, field);
        return true;
    }
}