using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[ExecuteAlways]
public class PrefabLightmapData : MonoBehaviour
{

    [System.Serializable]
    struct RendererInfo
    {
        public Renderer renderer;
        public int lightmapIndex;
        public Vector4 lightmapOffsetScale;
    }
    [System.Serializable]
    struct LightInfo
    {
        public Light light;
        public int lightmapBaketype;
        public int mixedLightingMode;
    }

    [SerializeField]
    RendererInfo[] m_RendererInfo;
    [SerializeField]
    Texture2D[] m_Lightmaps;
    [SerializeField]
    Texture2D[] m_LightmapsDir;
    [SerializeField]
    Texture2D[] m_ShadowMasks;
    [SerializeField]
    LightInfo[] m_LightInfo;


    void Start()
    {
        Init();
    }

    void Init()
    {
        if (m_RendererInfo == null || m_RendererInfo.Length == 0)
            return;

        var lightmaps = LightmapSettings.lightmaps;
        int[] offsetsindexes = new int[m_Lightmaps.Length];
        int counttotal = lightmaps.Length;
        List<LightmapData> combinedLightmaps = new List<LightmapData>();

        for (int i = 0; i < m_Lightmaps.Length; i++)
        {
            bool exists = false;
            for (int j = 0; j < lightmaps.Length; j++)
            {

                if (m_Lightmaps[i] == lightmaps[j].lightmapColor)
                {
                    exists = true;
                    offsetsindexes[i] = j;

                }

            }
            if (!exists)
            {
                offsetsindexes[i] = counttotal;
                var newlightmapdata = new LightmapData
                {
                    lightmapColor = m_Lightmaps[i],
                    lightmapDir = m_LightmapsDir.Length == m_Lightmaps.Length ? m_LightmapsDir[i] : default(Texture2D),
                    shadowMask = m_ShadowMasks.Length == m_Lightmaps.Length ? m_ShadowMasks[i] : default(Texture2D),
                };

                combinedLightmaps.Add(newlightmapdata);

                counttotal += 1;


            }

        }

        var combinedLightmaps2 = new LightmapData[counttotal];

        lightmaps.CopyTo(combinedLightmaps2, 0);
        combinedLightmaps.ToArray().CopyTo(combinedLightmaps2, lightmaps.Length);

        bool directional = true;

        foreach (Texture2D t in m_LightmapsDir)
        {
            if (t == null)
            {
                directional = false;
                break;
            }
        }

        LightmapSettings.lightmapsMode = (m_LightmapsDir.Length == m_Lightmaps.Length && directional) ? LightmapsMode.CombinedDirectional : LightmapsMode.NonDirectional;
        LightmapSettings.lightmaps = combinedLightmaps2;
        ApplyRendererInfo(m_RendererInfo, offsetsindexes, m_LightInfo);
    }

    void OnEnable()
    {

        SceneManager.sceneLoaded += OnSceneLoaded;

    }

    // called second
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Init();
    }

    // called when the game is terminated
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }



    void ApplyRendererInfo(RendererInfo[] infos, int[] lightmapOffsetIndex, LightInfo[] lightsInfo)
    {
        for (int i = 0; i < infos.Length; i++)
        {
            var info = infos[i];

            info.renderer.lightmapIndex = lightmapOffsetIndex[info.lightmapIndex];
            info.renderer.lightmapScaleOffset = info.lightmapOffsetScale;
        }

        for (int i = 0; i < lightsInfo.Length; i++)
        {
            if (lightsInfo[i].light == null)
                continue;

            var bakingOutput = lightsInfo[i].light.bakingOutput;

            bakingOutput.isBaked = true;
            bakingOutput.lightmapBakeType =
                (LightmapBakeType)lightsInfo[i].lightmapBaketype;

            bakingOutput.mixedLightingMode =
                (MixedLightingMode)lightsInfo[i].mixedLightingMode;

            lightsInfo[i].light.bakingOutput = bakingOutput;
        }


    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Bake Prefab Lightmaps")]
    static void GenerateLightmapInfo()
    {
        if (LightmapSettings.lightmaps == null ||
            LightmapSettings.lightmaps.Length == 0)
        {
            Debug.LogError("No baked lightmaps found. Bake lighting first.");
            return;
        }

        PrefabLightmapData[] prefabs = FindObjectsByType<PrefabLightmapData>(FindObjectsSortMode.None);

        foreach (var instance in prefabs)
        {
            var gameObject = instance.gameObject;
            var rendererInfos = new List<RendererInfo>();
            var lightmaps = new List<Texture2D>();
            var lightmapsDir = new List<Texture2D>();
            var shadowMasks = new List<Texture2D>();
            var lightsInfos = new List<LightInfo>();

            GenerateLightmapInfo(gameObject, rendererInfos, lightmaps, lightmapsDir, shadowMasks, lightsInfos);

            instance.m_RendererInfo = rendererInfos.ToArray();
            instance.m_Lightmaps = lightmaps.ToArray();
            instance.m_LightmapsDir = lightmapsDir.ToArray();
            instance.m_LightInfo = lightsInfos.ToArray();
            instance.m_ShadowMasks = shadowMasks.ToArray();
#if UNITY_2018_3_OR_NEWER
            var targetPrefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance.gameObject) as GameObject;
            if (targetPrefab != null)
            {
                GameObject root = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(instance.gameObject);
                if (root != null)
                {
                    GameObject rootPrefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(instance.gameObject);
                    string rootPath = UnityEditor.AssetDatabase.GetAssetPath(rootPrefab);

                    UnityEditor.PrefabUtility.UnpackPrefabInstanceAndReturnNewOutermostRoots(root, UnityEditor.PrefabUnpackMode.OutermostRoot);
                    try
                    {
                        UnityEditor.PrefabUtility.ApplyPrefabInstance(instance.gameObject, UnityEditor.InteractionMode.AutomatedAction);
                    }
                    catch { }
                    finally
                    {
                        UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(root, rootPath, UnityEditor.InteractionMode.AutomatedAction);
                    }
                }
                else
                {
                    UnityEditor.PrefabUtility.ApplyPrefabInstance(instance.gameObject, UnityEditor.InteractionMode.AutomatedAction);
                }
            }
#else
            var targetPrefab = UnityEditor.PrefabUtility.GetPrefabParent(gameObject) as GameObject;
            if (targetPrefab != null)
            {
                //UnityEditor.Prefab
                UnityEditor.PrefabUtility.ReplacePrefab(gameObject, targetPrefab);
            }
#endif
        }


    }

    static void GenerateLightmapInfo(GameObject root, List<RendererInfo> rendererInfos, List<Texture2D> lightmaps, List<Texture2D> lightmapsDir, List<Texture2D> shadowMasks, List<LightInfo> lightsInfo)
    {
        var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer.lightmapIndex != -1)
            {
                RendererInfo info = new RendererInfo();
                info.renderer = renderer;

                if (renderer.lightmapScaleOffset != Vector4.zero)
                {
                    //1ibrium's pointed out this issue : https://docs.unity3d.com/ScriptReference/Renderer-lightmapIndex.html
                    if (renderer.lightmapIndex < 0 || renderer.lightmapIndex == 0xFFFE) continue;
                    info.lightmapOffsetScale = renderer.lightmapScaleOffset;

                    Texture2D lightmap = LightmapSettings.lightmaps[renderer.lightmapIndex].lightmapColor;
                    Texture2D lightmapDir = LightmapSettings.lightmaps[renderer.lightmapIndex].lightmapDir;
                    Texture2D shadowMask = LightmapSettings.lightmaps[renderer.lightmapIndex].shadowMask;

                    info.lightmapIndex = lightmaps.IndexOf(lightmap);
                    if (info.lightmapIndex == -1)
                    {
                        info.lightmapIndex = lightmaps.Count;
                        lightmaps.Add(lightmap);
                        lightmapsDir.Add(lightmapDir);
                        shadowMasks.Add(shadowMask);
                    }

                    rendererInfos.Add(info);
                }

            }
        }

        var lights = root.GetComponentsInChildren<Light>(true);

        foreach (Light l in lights)
        {
            // Ignore realtime lights
            if (l.lightmapBakeType == LightmapBakeType.Realtime)
                continue;

            LightInfo lightInfo = new LightInfo();

            lightInfo.light = l;

            // Preserve Baked / Mixed
            lightInfo.lightmapBaketype = (int)l.lightmapBakeType;

            // Read directly from the light's baked output
            lightInfo.mixedLightingMode = (int)l.bakingOutput.mixedLightingMode;

            lightsInfo.Add(lightInfo);
        }
    }
#endif

}