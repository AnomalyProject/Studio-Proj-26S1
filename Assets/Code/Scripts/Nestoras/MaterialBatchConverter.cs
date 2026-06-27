#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MaterialBatchConverter : EditorWindow
{
    [SerializeField] private List<Material> materials = new();

    private const string SOURCE_SHADER = "Shader Graphs/S_RGB_Masking";
    private const string TARGET_SHADER = "Universal Render Pipeline/Lit";

    [MenuItem("Tools/Convert Materials -> URP Lit")]
    static void Open()
    {
        GetWindow<MaterialBatchConverter>("Material Converter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Drop Materials Here", EditorStyles.boldLabel);

        SerializedObject so = new SerializedObject(this);
        SerializedProperty list = so.FindProperty("materials");

        EditorGUILayout.PropertyField(list, true);

        so.ApplyModifiedProperties();

        GUILayout.Space(10);

        if (GUILayout.Button("Convert")) Convert();
    }

    void Convert()
    {
        Shader urpLit = Shader.Find(TARGET_SHADER);

        if (urpLit == null)
        {
            Debug.LogError("URP/Lit shader not found.");
            return;
        }

        int converted = 0;

        foreach (var mat in materials)
        {
            if (mat == null) continue;

            if (mat.shader == null || mat.shader.name != SOURCE_SHADER) continue;

            Undo.RecordObject(mat, "Convert Material");

            Texture baseMap = mat.GetTexture("_BaseMap");

            Texture normal = mat.GetTexture("_Normal");

            Texture orm = mat.GetTexture("_ORM");

            Texture emissive = mat.GetTexture("_Emissive");

            Color baseColor = mat.GetColor("_BaseColor_Value");

            Color emissionColor = mat.HasProperty("_EmissiveColor") ? mat.GetColor("_EmissiveColor") : Color.black;

            float flatness = mat.GetFloat("_Flatness");

            float roughness = mat.GetFloat("_Roughness");

            float metallic = mat.GetFloat("_Metallic");

            bool useEmission = mat.HasProperty("_Use_Emissive") && mat.GetFloat("_Use_Emissive") > 0.5f;

            bool useOpacity = mat.HasProperty("_UseOpacity") && mat.GetFloat("_UseOpacity") > 0.5f;

            // Swap shader
            mat.shader = urpLit;

            // Base
            mat.SetTexture("_BaseMap", baseMap);
            mat.SetColor("_BaseColor", baseColor);

            // Normal
            if (normal)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_BumpScale", flatness);
                mat.EnableKeyword("_NORMALMAP");
            }

            // Specular workflow
            mat.SetFloat("_WorkflowMode", 0);

            Color spec = new Color(metallic, metallic, metallic, 1f);

            mat.SetColor("_SpecColor", spec);

            // Roughness -> Smoothness
            mat.SetFloat("_Smoothness", Mathf.Clamp01(1f - roughness));

            // ORM -> Occlusion
            if (orm)
            {
                mat.SetTexture("_OcclusionMap", orm);
                mat.SetFloat("_OcclusionStrength", 1f);
            }

            // Emission
            if (useEmission && emissive)
            {
                float emissivePower = mat.HasProperty("_Emissive_Power") ? mat.GetFloat("_Emissive_Power") : 1f;

                // URP stores intensity in HDR color
                Color hdrEmission = emissionColor * emissivePower;

                mat.SetTexture("_EmissionMap", emissive);

                mat.SetColor("_EmissionColor", hdrEmission);

                // Enable emission UI + GI
                mat.EnableKeyword("_EMISSION");

                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;

                DynamicGI.SetEmissive(null, hdrEmission);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");

                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }

            // Transparency
            if (useOpacity)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_AlphaClip", 1);
            }

            EditorUtility.SetDirty(mat);

            converted++;
        }

        AssetDatabase.SaveAssets();

        Debug.Log($"Converted {converted} materials.");
    }
}
#endif