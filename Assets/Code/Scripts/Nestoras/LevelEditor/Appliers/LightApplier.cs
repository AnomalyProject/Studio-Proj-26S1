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
public class LightApplier : IComponentApplier
{
    public Type TargetType => typeof(Light);
    private Dictionary<string, Action<Light, FieldSnapshot>> supportedFields { get; } = new Dictionary<string, Action<Light, FieldSnapshot>>()
    {
        { "m_Type", (l, field) => l.type = (LightType)field.GetAs<int>() },
        { "m_Color", (l, field) => l.color = field.GetAs<Color>() },
        { "m_Intensity", (l, field) => l.intensity = field.GetAs<float>() },
        { "m_Range", (l, field) => l.range = field.GetAs<float>() },
        { "m_SpotAngle", (l, field) => l.spotAngle = field.GetAs<float>() },
        { "m_InnerSpotAngle", (l, field) => l.innerSpotAngle = field.GetAs<float>() },
        { "m_BounceIntensity", (l, field) => l.bounceIntensity = field.GetAs<float>() },
        { "m_CookieSize2D", (l, field) => l.cookieSize2D = field.GetAs<Vector2>() },
        { "m_Shadows.m_Type", (l, field) => l.shadows = (LightShadows)field.GetAs<int>() },
        { "m_Cookie", (l, field) => l.cookie = field.GetAsObject() as Texture },
        { "m_Flare", (l, field) => l.flare = field.GetAsObject() as Flare },
        { "m_CullingMask", (l, field) => l.cullingMask = field.GetAs<int>() },
        { "m_RenderMode", (l, field) => l.renderMode = (LightRenderMode)field.GetAs<int>() },
        { "m_RenderingLayerMask", (l, field) => l.renderingLayerMask = field.GetAs<int>() },
        { "m_LightShadowCasterMode", (l, field) => l.lightShadowCasterMode = (LightShadowCasterMode)field.GetAs<int>() },
        { "m_AreaSize", (l, field) => l.areaSize = field.GetAs<Vector2>() },
        { "m_BoundingSphereOverride", (l, field) => l.boundingSphereOverride = field.GetAs<Vector4>() },
        { "m_UseBoundingSphereOverride", (l, field) => l.useBoundingSphereOverride = field.GetAs<bool>() },
        { "m_UseViewFrustumForShadowCasterCull", (l, field) => l.useViewFrustumForShadowCasterCull = field.GetAs<bool>() },
        { "m_ForceVisible", (l, field) => l.forceVisible = field.GetAs<bool>() },
        { "m_LightUnit", (l, field) => l.lightUnit = (LightUnit)field.GetAs<int>() },
        { "m_LuxAtDistance", (l, field) => l.luxAtDistance = field.GetAs<float>() },
        { "m_EnableSpotReflector", (l, field) => l.enableSpotReflector = field.GetAs<bool>() },
        { "m_RenderingLayersMask", (l, field) => l.renderingLayerMask = field.GetAs<int>() },
        { "m_UseColorTemperature", (l, field) => l.useColorTemperature = field.GetAs<bool>() },
        { "m_ColorTemperature", (l, field) => l.colorTemperature = field.GetAs<float>() },
    };
    private HashSet<string> ignoredFields { get; } = new HashSet<string>()
    {
        "m_DrawHalo",
        "m_ShadowRenderingLayersMask",
        "m_Lightmapping",
        "m_Shadows",
        "m_ShadowAngle",
        "m_ShadowRadius",
        "m_Shadows.m_Resolution",
        "m_Shadows.m_CustomResolution",
        "m_Shadows.m_Strength",
        "m_Shadows.m_Bias",
        "m_Shadows.m_NormalBias",
        "m_Shadows.m_NearPlane",
        "m_Shadows.m_UseCullingMatrixOverride",

    };
    public bool Supports(string path) => supportedFields.ContainsKey(path);
    public bool Ignores(string path) => ignoredFields.Contains(path) || path.StartsWith("m_Shadows.m_CullingMatrixOverride") || path.StartsWith("m_BakingOutput.");
    public bool Apply(Component target, FieldSnapshot field)
    {
        if (!supportedFields.ContainsKey(field.path)) return false;
        supportedFields[field.path]((Light)target, field);
        return true;
    }
}