using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NarrationEntry))]
public class NarrationEntryEditor : Editor
{
    private bool paused;
    private static System.Type AudioUtil => typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

    private static void PlayClip(AudioClip clip) => AudioUtil.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public,
            null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null)
            ?.Invoke(null, new object[] { clip, 0, false });

    private static void PauseClip() => AudioUtil.GetMethod("PausePreviewClip", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
    private static void ResumeClip() => AudioUtil.GetMethod("ResumePreviewClip", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
    private static void StopClip() => AudioUtil.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
    private static float ClipPosition() => (float)(AudioUtil.GetMethod("GetPreviewClipPosition", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null) ?? 0f);
    private static bool IsPlaying() => (bool)(AudioUtil.GetMethod("IsPreviewClipPlaying", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null) ?? false);
    private void OnEnable() => EditorApplication.update += Repaint;
    private void OnDisable() 
    { 
        EditorApplication.update -= Repaint; 
        StopClip(); 
        paused = false; 
    }
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NarrationEntry entry = (NarrationEntry)target;
        AudioClip clip = entry.VoiceClip;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(clip == null))
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("▶ Play"))
            {
                StopClip();
                PlayClip(clip);
                paused = false;
            }

            // Toggle pause/resume on repeated presses
            string pauseLabel = paused ? "▶ Resume" : "⏸ Pause";
            if (GUILayout.Button(pauseLabel))
            {
                if (paused) { ResumeClip(); paused = false; }
                else { PauseClip(); paused = true; }
            }

            if (GUILayout.Button("⏹ Stop"))
            {
                StopClip();
                paused = false;
            }

            EditorGUILayout.EndHorizontal();
        }

        if (clip == null)
        {
            EditorGUILayout.HelpBox("Assign a Voice Clip to enable preview.", MessageType.Info);
            return;
        }

        // Current position display
        float t = ClipPosition();
        if (IsPlaying() || paused)
        {
            int seconds = (int)t;
            int milliseconds = (int)((t - seconds) * 100f);
            EditorGUILayout.LabelField($"{seconds}:{milliseconds:D2}", EditorStyles.centeredGreyMiniLabel);
        }

        // Active subtitle cue
        SubtitleEntry? active = null;
        foreach (SubtitleEntry cue in entry.Subtitles)
        {
            if (cue.TimeStamp <= t) active = cue;
        }

        EditorGUILayout.Space(4);

        if (active.HasValue && (IsPlaying() || paused))
        {
            EditorGUILayout.LabelField($"[{active.Value.SpeakerName}]", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(active.Value.DialogueText, EditorStyles.wordWrappedLabel);
        }
        else
        {
            EditorGUILayout.LabelField("—", EditorStyles.centeredGreyMiniLabel);
        }
    }
}