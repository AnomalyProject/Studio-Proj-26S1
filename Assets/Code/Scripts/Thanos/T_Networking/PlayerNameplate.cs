using System;
using UnityEngine;
using TMPro;
using PurrNet;

public class PlayerNameplate : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject nameplateVisuals;
    //private SyncVar<String> username;
    private SyncVar<string> username = new();
    
    private void Awake()
    {
        username.onChanged += ApplyName;
    }

    private void OnDestroy()
    {
        username.onChanged -= ApplyName;
    }
    /// <summary>
    /// Called by the spawn system or player setup to set the name.
    /// Kept as a public method so the name can be set after the object is created.
    /// </summary>
    [ObserversRpc]
    public void SetName_Server(string displayName)
    {
        nameText.text = displayName;
    }
    
    public void SetVisible(bool visible)
    {
        nameplateVisuals.SetActive(visible);
    }
    
    private void ApplyName(string displayName)
    {
        if (nameText == null)
            return;

        nameText.text = string.IsNullOrWhiteSpace(displayName)
            ? "Loading..."
            : displayName;
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