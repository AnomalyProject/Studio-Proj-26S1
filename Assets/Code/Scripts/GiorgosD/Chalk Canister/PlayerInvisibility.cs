using System.Collections;
using System.Threading.Tasks;
using PurrNet;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

public class PlayerInvisibility : NetworkBehaviour
{
    [SerializeField] private GameObject bodyVisuals;
    
    [Header("Invisible Settings")]
    [SerializeField] private SyncVar<bool> isInvisible;
    [SerializeField] private float invisibleTimer = 5.0f;
    
    [SerializeField] private Volume PPInvisVolume;
    [SerializeField] private Material invisMat;
    [SerializeField, Tooltip("Lower values mean longer fade in.")] private float fadeOutSpeed = 2.0f;
    private Renderer playerRenderer;
    private Material[] originalMat;
    
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private float audioTransTime = 0.4f;
    private string snapshotNormal = "Normal";
    private string snapshotMuffled = "Muffled";
    
    public bool IsInvis => isInvisible.value;
    
    protected override void OnSpawned(bool asServer)
    {
        if (bodyVisuals == null) return;
        
        playerRenderer = bodyVisuals.GetComponentInChildren<Renderer>();
        
        if (playerRenderer != null)
        {
            originalMat = playerRenderer.materials;
        }
    }
    
    [ObserversRpc(bufferLast: true)]
    public void StartInvisTimer()
    {
        if (!isServer) return;
        
        StartCoroutine(InvisTimer());
    }

    [ObserversRpc]
    public async void EnablePPVolume(bool isActive, float targetWeight, float fadeSpeed)
    {
        if (!isOwner || PPInvisVolume == null) return;

        while (!Mathf.Approximately(PPInvisVolume.weight, targetWeight))
        {
            PPInvisVolume.weight = Mathf.MoveTowards(PPInvisVolume.weight, targetWeight, fadeSpeed * Time.deltaTime);
            
            await Task.Yield();
        }
        
        PPInvisVolume.weight = targetWeight;
    }

    private IEnumerator InvisTimer()
    {
        if (!isServer) yield break;
        
        isInvisible.value = true;
        ChangeMatRPC(true);
        yield return new WaitForSeconds(invisibleTimer);
        ChangeMatRPC(false); 
        EnablePPVolume(false, 0, fadeOutSpeed);
        isInvisible.value = false;
    }
    
    [ObserversRpc(bufferLast: true)]
    private void ChangeMatRPC(bool shouldBeInvis)
    {
        if (isOwner)
        {
            if (mainMixer != null)
            {
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
}
