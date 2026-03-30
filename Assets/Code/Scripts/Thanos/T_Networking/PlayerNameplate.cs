using System;
using UnityEngine;
using TMPro;
using PurrNet;

public class PlayerNameplate : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject nameplateVisuals;
    
    /// <summary>
    /// Called by the spawn system or player setup to set the name.
    /// Kept as a public method so the name can be set after the object is created.
    /// </summary>
    [ObserversRpc]
    public void SetName(string displayName)
    {
        nameText.text = displayName;
    }
    
    public void SetVisible(bool visible)
    {
        nameplateVisuals.SetActive(visible);
    }

    // chose late update to avoid jittering
    private void LateUpdate()
    {
        if (nameplateVisuals == null || !nameplateVisuals.activeSelf) return;
        
        // Camera.main returns the active camera to THIS machine
        // so each client's active camera
        Camera camera = Camera.main;
        if (camera == null) return;

        // rotate nameplate to face the camera
        transform.forward = camera.transform.forward;
    }
}