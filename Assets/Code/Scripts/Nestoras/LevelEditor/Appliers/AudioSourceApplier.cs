using System.Collections.Generic;
using System;
using static SnapshotUtility;
using UnityEngine.Scripting;
using UnityEngine.Audio;
using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Applier script to translate set commands by <see cref="ModificationApplier"/> (SerializedProperty path and value) into actual API calls that work in standalone builds.
/// </summary>
[Preserve] // Avoid stripping type from build
public class AudioSourceApplier : IComponentApplier
{
    public Type TargetType => typeof(AudioSource);
    private Dictionary<string, Action<AudioSource, FieldSnapshot>> supportedFields { get; } = new Dictionary<string, Action<AudioSource, FieldSnapshot>>()
    {
        { "OutputAudioMixerGroup", (s, field) => s.outputAudioMixerGroup = field.GetAsObject() as AudioMixerGroup },
        { "m_audioClip", (s, field) => s.clip = field.GetAsObject() as AudioClip },
        { "m_Resource", (s, field) => s.resource = field.GetAsObject() as AudioResource },
        { "m_PlayOnAwake", (s, field) => s.playOnAwake = field.GetAs<bool>() },
        { "m_Volume", (s, field) => s.volume = field.GetAs<float>() },
        { "m_Pitch", (s, field) => s.pitch = field.GetAs<float>() },
        { "Loop", (s, field) => s.loop = field.GetAs<bool>() },
        { "Mute", (s, field) => s.mute = field.GetAs<bool>() },
        { "Spatialize", (s, field) => s.spatialize = field.GetAs<bool>() },
        { "SpatializePostEffects", (s, field) => s.spatializePostEffects = field.GetAs<bool>() },
        { "Priority", (s, field) => s.priority = field.GetAs<int>() },
        { "DopplerLevel", (s, field) => s.dopplerLevel = field.GetAs<float>() },
        { "MinDistance", (s, field) => s.minDistance = field.GetAs<float>() },
        { "MaxDistance", (s, field) => s.maxDistance = field.GetAs<float>() },
        { "Pan2D", (s, field) => s.panStereo = field.GetAs<float>() },
        { "rolloffMode", (s, field) => s.rolloffMode = (AudioRolloffMode)field.GetAs<int>() },
        { "BypassEffects", (s, field) => s.bypassEffects = field.GetAs<bool>() },
        { "BypassListenerEffects", (s, field) => s.bypassListenerEffects = field.GetAs<bool>() },
        { "BypassReverbZones", (s, field) => s.bypassReverbZones = field.GetAs<bool>() },
        { "rolloffCustomCurve", (s, field) => s.SetCustomCurve(AudioSourceCurveType.CustomRolloff, field.GetAs<AnimationCurve>()) },
        { "panLevelCustomCurve", (s, field) => s.SetCustomCurve(AudioSourceCurveType.SpatialBlend, field.GetAs<AnimationCurve>()) },
        { "spreadCustomCurve", (s, field) => s.SetCustomCurve(AudioSourceCurveType.Spread, field.GetAs<AnimationCurve>()) },
        { "reverbZoneMixCustomCurve", (s, field) => s.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, field.GetAs<AnimationCurve>()) },
    };
    public bool Supports(string path) => supportedFields.ContainsKey(path);
    public bool Ignores(string path) => false;
    public bool Apply(Component target, FieldSnapshot field)
    {
        if (!supportedFields.ContainsKey(field.path)) return false;
        supportedFields[field.path]((AudioSource)target, field);
        return true;
    }
}