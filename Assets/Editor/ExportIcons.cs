#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class ExportIcons
{
    //[MenuItem("Tools/Export Console Icons")]
    static void Export()
    {
        //Save("console.infoicon", "log.png");
        //Save("console.warnicon", "warning.png");
        //Save("console.erroricon", "error.png");
        //Save("RotateTool", "rotate.png");
    }

    static void Save(string iconName, string fileName)
    {
        GUIContent content = EditorGUIUtility.IconContent(iconName);
        Texture2D source = content.image as Texture2D;

        Texture2D readableTex = MakeReadable(source);

        var bytes = readableTex.EncodeToPNG();
        File.WriteAllBytes("Assets/Resources/DevConsole/" + fileName, bytes);

        Debug.Log("Saved: " + fileName);
    }

    static Texture2D MakeReadable(Texture2D source)
    {
        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

        Graphics.Blit(source, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readable = new Texture2D(source.width, source.height);
        readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        readable.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return readable;
    }
}
#endif