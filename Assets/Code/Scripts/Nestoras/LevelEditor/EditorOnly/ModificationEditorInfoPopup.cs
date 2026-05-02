#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Popup window showing important information about the modification system, such as how appliers work, how references are stored, and how additions / removals are handled.
/// </summary>
public class ModificationEditorInfoPopup : EditorWindow
{
    private Vector2 scroll;

    private static GUIStyle header;
    private static GUIStyle body;

    public static void ShowWindow()
    {
        header = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 };
        body = new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true, fontSize = 12 };

        ModificationEditorInfoPopup window = CreateInstance<ModificationEditorInfoPopup>();

        // Centered default position
        Rect main = EditorGUIUtility.GetMainWindowPosition();
        float width = 700;
        float height = 560;

        window.position = new Rect(main.x + (main.width - width) / 2, main.y + (main.height - height) / 2, width, height);
        window.minSize = new Vector2(400, 300);
        window.titleContent = new GUIContent("Info");
        window.ShowUtility();
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        GUILayout.Label("Appliers", header);
        GUILayout.Label("<line-indent=2em>Many built-in components like Renderer or Transform have fields that can't be accessed at runtime (e.g. \"m_localPosition\"). To handle this, special <b><color=#6FB76F>IComponentApplier</b></color>s have to be written <b>for each native component</b>. If the system tries to update a field without an applier, it will attempt to use reflections to update it, <color=yellow>but might fail</color>. It is also <b>slower</b>, so if speed is important for a custom Component, you can create an applier for it.</line-indent>", body);
        GUILayout.Space(10);
        GUILayout.Label("Additions / Removals", header);
        GUILayout.Label("<line-indent=2em>The order of children in a GameObject hierarchy is not preserved when serializing object additions / removals; just their parent / children relationship. However, components are indexed by their <b><u>order on the GameObject</u></b>. If that order changes, it could break the system.\nAdding / Deleting an object forces the system to take a snapshot of all its fields. That means a bigger file size and slower application / reversal; prefer disabling / enabling when you can. To minimize file sizes and performance impact, when you HAVE to add a complex hierarchy of objects to the scene, you can create a script that spawns a prefab. A manually added prefab will not be treated as a referenced object, so it will be re-serialized and take up twice the space on disk.</line-indent>", body);
        GUILayout.Space(10);
        GUILayout.Label("References", header);
        GUILayout.Label("<line-indent=2em>Whenever you change an object reference, if that reference <b>isn't a file</b>, but an object in the scene (GameObject or Component), then the system <color=#FF5555><b>can't store it</b></color>, as it only exists for the lifetime of the scene instance.\nIf the reference <b>is a file</b> (Mesh, Material, Shader, AudioClip, etc.), then it is assigned a GUID and referenced in a registry inside the <b>Resources</b> folder (if you later delete that modification, then the registry will have to be <b>manually cleared</b> of any entries for unused references).</line-indent>", body);
        GUILayout.Space(10);
        GUILayout.Label("Exported Prefabs", header);
        GUILayout.Label("<line-indent=2em>After finishing a level, you may drag it to the project tab to export it as a prefab. <color=#FF5555><b>NEVER</b></color> modify the prefab directly. <color=#6FB76F><b>ALWAYS</b></color> bring it back into a scene to edit it again. Failing to do so may de-sync some critical data. After you're done, don't forget to apply your overrides to the prefab.</line-indent>", body);
        GUILayout.Space(10);
        GUILayout.Label("Particle Systems", header);
        GUILayout.Label("<line-indent=2em><color=#FF5555><b>Particles are BAD and I HATE them.</b></color> Do <b>NOT</b> modify particle systems. If you do, the console will log <b>600 different warnings</b> because you <b><u>deserve it</u></b>. If you <b>HAVE</b> to instantiate a particle system instead of just toggling it, convert it to a prefab and use a spawner. Also, adding / removing particle systems will make your modification asset larger than your mom.</line-indent>", body);
        EditorGUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Open Documentation", GUILayout.MaxWidth(150))) Application.OpenURL("https://app.clickup.com/90151230104/v/dc/2kypx6mr-4495/2kypx6mr-15595");
        GUILayout.EndHorizontal();
    }
}
#endif