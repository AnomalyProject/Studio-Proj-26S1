using System.Collections.Generic;
using System;
using static SnapshotUtility;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Applier script to translate set commands by <see cref="ModificationApplier"/> (SerializedProperty path and value) into actual API calls that work in standalone builds.
/// </summary>
[Preserve] // Avoid stripping type from build
public class RendererApplier : IComponentApplier
{
    public Type TargetType => typeof(Renderer);
    private Dictionary<string, Action<Renderer, FieldSnapshot>> supportedFields { get; } = new Dictionary<string, Action<Renderer, FieldSnapshot>>()
    {
        { "m_CastShadows", (r, field) => r.shadowCastingMode = (ShadowCastingMode)field.GetAs<int>() },
        { "m_ReceiveShadows", (r, field) => r.receiveShadows = field.GetAs<bool>() },
        { "m_MotionVectors", (r, field) => r.motionVectorGenerationMode = (MotionVectorGenerationMode)field.GetAs<int>() },
        { "m_LightProbeUsage", (r, field) => r.lightProbeUsage = (LightProbeUsage)field.GetAs<int>() },
        { "m_RenderingLayerMask", (r, field) => r.renderingLayerMask = (uint)field.GetAs<int>() },
        { "m_RendererPriority", (r, field) => r.rendererPriority = field.GetAs<int>() },
        { "m_ProbeAnchor", (r, field) => r.probeAnchor = field.GetAsObject() as Transform },
        { "m_DynamicOccludee", (r, field) => r.allowOcclusionWhenDynamic = field.GetAs<bool>() },
        { "m_StaticShadowCaster", (r, field) => r.staticShadowCaster = field.GetAs<bool>() },
        { "m_ReflectionProbeUsage", (r, field) => r.reflectionProbeUsage = (ReflectionProbeUsage)field.GetAs<int>() },
        { "m_LightProbeVolumeOverride", (r, field) => r.lightProbeProxyVolumeOverride = field.GetAsObject() as GameObject },
    };
    private HashSet<string> ignoredFields { get; } = new HashSet<string>()
    {
        "m_Enabled",
        "m_RayTracingMode",
        "m_RayTraceProcedural",
        "m_RayTracingAccelStructBuildFlagsOverride",
        "m_RayTracingAccelStructBuildFlags",
        "m_SmallMeshCulling",
        "m_ForceMeshLod",
        "m_MeshLodSelectionBias",
        "m_ScaleInLightmap",
        "m_ReceiveGI",
        "m_PreserveUVs",
        "m_IgnoreNormalsForChartDetection",
        "m_ImportantGI",
        "m_StitchLightmapSeams",
        "m_MinimumChartSize",
        "m_AutoUVMaxDistance",
        "m_AutoUVMaxAngle",
        "m_LightmapParameters",
        "m_Materials",
        "m_Materials.Array.size",
        "m_MaskInteraction",
    };
    public bool Supports(string path) => supportedFields.ContainsKey(path) || path.StartsWith("m_Materials.Array.data[");
    public bool Ignores(string path) => ignoredFields.Contains(path);
    public bool Apply(Component target, FieldSnapshot field)
    {
        // Small use of reflection to support material array elements, but trying to keep it speedy.
        Renderer r = (Renderer)target;
        if (field.path.StartsWith("m_Materials.Array.data["))
        {
            int start = field.path.IndexOf('[') + 1;
            int end = field.path.IndexOf(']');
            int index = int.Parse(field.path.Substring(start, end - start));

            Material[] mats = r.sharedMaterials;
            if (index >= mats.Length) Array.Resize(ref mats, index + 1);
            mats[index] = field.GetAsObject() as Material;
            r.sharedMaterials = mats;
            return true;
        }

        if (!supportedFields.ContainsKey(field.path)) return false;
        supportedFields[field.path]((Renderer)target, field);
        return true;
    }
}