using UnityEngine;
using System;

public class PrefabLightmapData : MonoBehaviour
{
    [Serializable]
    public class RendererLightmapData
    {
        public Renderer renderer;
        public int lightmapIndex;
        public Vector4 scaleOffset;
    }

    [SerializeField] private RendererLightmapData[] renderers;

    [ContextMenu("Capture Lightmap Data")]
    public void CaptureLightmapData()
    {
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);

        renderers = new RendererLightmapData[allRenderers.Length];

        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer r = allRenderers[i];

            renderers[i] = new RendererLightmapData
            {
                renderer = r,
                lightmapIndex = r.lightmapIndex,
                scaleOffset = r.lightmapScaleOffset
            };

#if UNITY_EDITOR
            Debug.Log($"{r.name} -> Index:{r.lightmapIndex} Offset:{r.lightmapScaleOffset}", r);
#endif
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Apply Lightmap Data")]
    public void ApplyLightmapData()
    {
        foreach (RendererLightmapData data in renderers)
        {
            if (data.renderer == null) continue;

            data.renderer.lightmapIndex = data.lightmapIndex;
            data.renderer.lightmapScaleOffset = data.scaleOffset;
        }
    }

    private void Awake() => ApplyLightmapData();
}