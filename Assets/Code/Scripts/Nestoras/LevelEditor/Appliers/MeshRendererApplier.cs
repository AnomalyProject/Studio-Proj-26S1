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
public class MeshRendererApplier : IComponentApplier
{
    public Type TargetType => typeof(MeshRenderer);
    public HashSet<string> supportedFields { get; } = new HashSet<string>()
    {
        "m_CastShadows",
        "m_ReceiveShadows",
        "m_MotionVectors",
        "m_LightProbeUsage",
        "m_RenderingLayerMask",

        "m_RendererPriority",
        "m_ProbeAnchor",

        "m_DynamicOccludee",
        "m_StaticShadowCaster",
        "m_ReflectionProbeUsage",
        "m_LightProbeVolumeOverride",
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
        "m_Materials.Array.size",
        "m_MaskInteraction",

        "m_Materials",
    };

    public bool Supports(string path) => supportedFields.Contains(path) || ignoredFields.Contains(path) || path.StartsWith("m_Materials.Array.data[");

    public bool Apply(Component target, FieldSnapshot field)
    {
        if (ignoredFields.Contains(field.path)) return true;

        MeshRenderer r = (MeshRenderer)target;

        switch (field.path)
        {
            case "m_CastShadows":
                r.shadowCastingMode = (ShadowCastingMode)field.GetAs<int>();
                return true;
            case "m_ReceiveShadows":
                r.receiveShadows = field.GetAs<bool>();
                return true;
            case "m_RenderingLayerMask":
                r.renderingLayerMask = (uint)field.GetAs<int>();
                return true;
            case "m_RendererPriority":
                r.rendererPriority = field.GetAs<int>();
                return true;
            case "m_LightProbeUsage":
                r.lightProbeUsage = (LightProbeUsage)field.GetAs<int>();
                return true;
            case "m_ProbeAnchor":
                r.probeAnchor = field.GetAsObject() as Transform;
                return true;
            case "m_DynamicOccludee":
                r.allowOcclusionWhenDynamic = field.GetAs<bool>();
                return true;
            case "m_StaticShadowCaster":
                r.staticShadowCaster = field.GetAs<bool>();
                return true;
            case "m_MotionVectors":
                r.motionVectorGenerationMode = (MotionVectorGenerationMode)field.GetAs<int>();
                return true;
            case "m_ReflectionProbeUsage":
                r.reflectionProbeUsage = (ReflectionProbeUsage)field.GetAs<int>();
                return true;
            case "m_LightProbeVolumeOverride":
                r.lightProbeProxyVolumeOverride = field.GetAsObject() as GameObject;
                return true;
        }

        // Handle materials dynamically
        if (field.path.StartsWith("m_Materials.Array.data["))
        {
            int start = field.path.IndexOf('[') + 1;
            int end = field.path.IndexOf(']');
            int index = int.Parse(field.path.Substring(start, end - start));

            var mats = r.sharedMaterials;

            if (index >= mats.Length) Array.Resize(ref mats, index + 1);

            mats[index] = field.GetAsObject() as Material;

            r.sharedMaterials = mats;

            return true;
        }

        return false;
    }
}