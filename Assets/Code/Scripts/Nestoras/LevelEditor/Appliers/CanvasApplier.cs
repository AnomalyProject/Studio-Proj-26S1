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
public class CanvasApplier : IComponentApplier
{
    public Type TargetType => typeof(Canvas);
    private Dictionary<string, Action<Canvas, FieldSnapshot>> supportedFields { get; } = new Dictionary<string, Action<Canvas, FieldSnapshot>>()
    {
        { "m_RenderMode", (c, field) => c.renderMode = (RenderMode)field.GetAs<int>() },
        { "m_Camera", (c, field) => c.worldCamera = field.GetAsObject() as Camera },
        { "m_PlaneDistance", (c, field) => c.planeDistance = field.GetAs<float>() },
        { "m_PixelPerfect", (c, field) => c.pixelPerfect = field.GetAs<bool>() },
        { "m_OverrideSorting", (c, field) => c.overrideSorting = field.GetAs<bool>() },
        { "m_OverridePixelPerfect", (c, field) => c.overridePixelPerfect = field.GetAs<bool>() },
        { "m_SortingBucketNormalizedSize", (c, field) => c.normalizedSortingGridSize = field.GetAs<float>() },
        { "m_VertexColorAlwaysGammaSpace", (c, field) => c.vertexColorAlwaysGammaSpace = field.GetAs<bool>() },
        { "m_AdditionalShaderChannelsFlag", (c, field) => c.additionalShaderChannels = (AdditionalCanvasShaderChannels)field.GetAs<int>() },
        { "m_UpdateRectTransformForStandalone", (c, field) => c.updateRectTransformForStandalone = (StandaloneRenderResize)field.GetAs<int>() },
    };
    private HashSet<string> ignoredFields { get; } = new HashSet<string>() { "m_ReceivesEvents" };
    public bool Supports(string path) => supportedFields.ContainsKey(path);
    public bool Ignores(string path) => ignoredFields.Contains(path);
    public bool Apply(Component target, FieldSnapshot field)
    {
        if (!supportedFields.ContainsKey(field.path)) return false;
        supportedFields[field.path]((Canvas)target, field);
        return true;
    }
}