using System.Collections;
using PurrNet;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

public class PlayerInvisibility : NetworkBehaviour
{
    [SerializeField] private GameObject bodyVisuals;
    
    [Header("Invisible Settings")]
    [SerializeField] private float invisibleTimer = 5.0f;
    
    [SerializeField] private Volume PPInvisVolume;
    [SerializeField] private Material invisMat;
    [SerializeField, Tooltip("Lower values mean longer fade in.")] private float fadeSpeed = 2.0f;
    private Renderer playerRenderer;
    private Material[] originalMat;
    
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private float audioTransTime = 0.4f;
    private SyncVar<bool> isInvisible = new(initialValue: false, ownerAuth: false);
    private string snapshotNormal = "Normal";
    private string snapshotMuffled = "Muffled";
    private int targetWeight;

    public bool IsInvis => isInvisible.value;
    
    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        if (bodyVisuals == null) return;
        
        playerRenderer = bodyVisuals.GetComponentInChildren<Renderer>();
        
        if (playerRenderer != null)
        {
            originalMat = playerRenderer.materials;
        }

        isInvisible.onChanged += ApplyInvisibleEffects;
        PPInvisVolume.enabled = isOwner;
    }

    [ServerRpc] public void StartInvisTimer()
    {
        if (!isServer) return;       
        StartCoroutine(InvisTimer());
    }

    public async void EnablePPVolume(bool isActive, float fadeSpeed)
    {
        if (!isOwner || PPInvisVolume == null) return;

        targetWeight = isActive ? 1 : 0;

        while (!Mathf.Approximately(PPInvisVolume.weight, targetWeight))
        {
            PPInvisVolume.weight = Mathf.MoveTowards(PPInvisVolume.weight, targetWeight, fadeSpeed * Time.deltaTime);

            await Awaitable.NextFrameAsync();
        }
        
        PPInvisVolume.weight = targetWeight;
    }

    private IEnumerator InvisTimer()
    {
        if (!isServer || isInvisible.value) yield break;
        
        isInvisible.value = true;
        yield return new WaitForSeconds(invisibleTimer);
        isInvisible.value = false;
    }

    private void ApplyInvisibleEffects(bool shouldBeInvis)
    {
        if (isOwner)
        {
            if (mainMixer != null)
            {
                EnablePPVolume(isActive: shouldBeInvis, fadeSpeed: fadeSpeed);
                string targetSnapshot = shouldBeInvis ? snapshotMuffled : snapshotNormal;
                AudioMixerSnapshot snapshot = mainMixer.FindSnapshot(targetSnapshot);
                if (snapshot != null)
                {
                    snapshot.TransitionTo(audioTransTime);
                }
            }
        }
        else
        {
            if (playerRenderer != null && invisMat != null)
            {
                if (shouldBeInvis)
                {
                    Material[] invisMats = new  Material[originalMat.Length];
                    for (int i = 0; i < invisMats.Length; i++)
                    {
                        invisMats[i] = invisMat;
                    }
                    playerRenderer.materials = invisMats;
                }
                else
                {
                    playerRenderer.materials = originalMat;
                }
            }
        }
    }

    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);

        if (asServer || !isOwner) return;
        AudioMixerSnapshot snapshot = mainMixer.FindSnapshot(snapshotNormal);
    }
}