using System;
using UnityEngine;
using TMPro;
using PurrNet;

public class PlayerNameplate : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject nameplateVisuals;
    
    private FPSInputHandler inputHandler;
    private Camera targetCamera;
    
    private void Awake()
    {
        inputHandler = GetComponentInParent<FPSInputHandler>();
    }

    void Start()
    {
        if (inputHandler == null) return;
        
        // hide nameplate for the owner
        if (inputHandler.isOwner)
        {
            nameplateVisuals.SetActive(false);
            enabled = false;
            return;
        }
    }
    
    /// <summary>
    /// Called by the spawn system or player setup to set the name.
    /// Kept as a public method so the name can be set after the object is created.
    /// </summary>
    public void SetName(string displayName)
    {
        nameText.text = displayName;
    }

    // chose late update to avoid jittering
    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            // Camera.main returns the active camera to THIS machine
            // so each client's active camera
            targetCamera = Camera.main;
            if (targetCamera == null) return;
        }

        // rotate nameplate to face the camera
        transform.forward = targetCamera.transform.forward;
    }
}