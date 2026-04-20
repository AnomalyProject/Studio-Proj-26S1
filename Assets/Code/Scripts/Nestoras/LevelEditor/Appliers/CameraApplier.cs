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
public class CameraApplier : IComponentApplier
{
    public Type TargetType => typeof(Camera);
    private Dictionary<string, Action<Camera, FieldSnapshot>> supportedFields { get; } = new Dictionary<string, Action<Camera, FieldSnapshot>>()
    {
        { "m_ClearFlags", (c, field) => c.clearFlags = (CameraClearFlags)field.GetAs<int>() },
        { "m_BackGroundColor", (c, field) => c.backgroundColor = field.GetAs<Color>() },
        { "m_projectionMatrixMode", (c, field) => c.usePhysicalProperties = field.GetAs<int>() == 2 },
        { "m_GateFitMode", (c, field) => c.gateFit = (Camera.GateFitMode)field.GetAs<int>() },
        { "m_Iso", (c, field) => c.iso = field.GetAs<int>() },
        { "m_Aperture", (c, field) => c.aperture = field.GetAs<float>() },
        { "m_FocusDistance", (c, field) => c.focusDistance = field.GetAs<float>() },
        { "m_FocalLength", (c, field) => c.focalLength = field.GetAs<float>() },
        { "m_BladeCount", (c, field) => c.bladeCount = field.GetAs<int>() },
        { "m_Curvature", (c, field) => c.curvature = field.GetAs<Vector2>() },
        { "m_BarrelClipping", (c, field) => c.barrelClipping = field.GetAs<float>() },
        { "m_Anamorphism", (c, field) => c.anamorphism = field.GetAs<float>() },
        { "m_SensorSize", (c, field) => c.sensorSize = field.GetAs<Vector2>() },
        { "m_LensShift", (c, field) => c.lensShift = field.GetAs<Vector2>() },
        { "m_NormalizedViewPortRect", (c, field) => c.rect = field.GetAs<Rect>() },
        { "m_ShutterSpeed", (c, field) => c.shutterSpeed = field.GetAs<float>() },
        { "near clip plane", (c, field) => c.nearClipPlane = field.GetAs<float>() },
        { "far clip plane", (c, field) => c.farClipPlane = field.GetAs<float>() },
        { "field of view", (c, field) => c.fieldOfView = field.GetAs<float>() },
        { "orthographic", (c, field) => c.orthographic = field.GetAs<bool>() },
        { "orthographic size", (c, field) => c.orthographicSize = field.GetAs<float>() },
        { "m_Depth", (c, field) => c.depth = field.GetAs<float>() },
        { "m_CullingMask", (c, field) => c.cullingMask = field.GetAs<int>() },
        { "m_RenderingPath", (c, field) => c.renderingPath = (RenderingPath)field.GetAs<int>() },
        { "m_TargetTexture", (c, field) => c.targetTexture = field.GetAsObject() as RenderTexture },
        { "m_TargetDisplay", (c, field) => c.targetDisplay = field.GetAs<int>() },
        { "m_TargetEye", (c, field) => c.stereoTargetEye = (StereoTargetEyeMask)field.GetAs<int>() },
        { "m_HDR", (c, field) => c.allowHDR = field.GetAs<bool>() },
        { "m_AllowMSAA", (c, field) => c.allowMSAA = field.GetAs<bool>() },
        { "m_AllowDynamicResolution", (c, field) => c.allowDynamicResolution = field.GetAs<bool>() },
        { "m_ForceIntoRT", (c, field) => c.forceIntoRenderTexture = field.GetAs<bool>() },
        { "m_OcclusionCulling", (c, field) => c.useOcclusionCulling = field.GetAs<bool>() },
        { "m_StereoConvergence", (c, field) => c.stereoConvergence = field.GetAs<float>() },
        { "m_StereoSeparation", (c, field) => c.stereoSeparation = field.GetAs<float>() },
    };
    public bool Supports(string path) => supportedFields.ContainsKey(path);
    public bool Ignores(string path) => path == "m_FOVAxisMode";
    public bool Apply(Component target, FieldSnapshot field)
    {
        if (!supportedFields.ContainsKey(field.path)) return false;
        supportedFields[field.path]((Camera)target, field);
        return true;
    }
}