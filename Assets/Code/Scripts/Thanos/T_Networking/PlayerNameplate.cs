using System;
using UnityEngine;
using TMPro;
using PurrNet;
using Steamworks;

public class PlayerNameplate : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject nameplateVisuals;
    
    /// <summary>
    /// Called by the spawn system or player setup to set the name.
    /// Kept as a public method so the name can be set after the object is created.
    /// </summary>
    [ObserversRpc(bufferLast: true)]
    public void SetName(string displayName, int colourIndex)
    {
        nameText.text = displayName;
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
    
    protected override void OnSpawned() 
    {
        if (!SteamManager.Initialized) return;
        
        ulong ownerSteamID = SteamUser.GetSteamID().m_SteamID;
        
        SessionData currentSession = SessionManager.Instance?.CurrentSession;
        PlayerSessionInfo? playerInfo = currentSession?.GetPlayer(ownerSteamID);

        if (playerInfo.HasValue)
        {
            nameText.text = playerInfo.Value.DisplayName;
            nameText.color = PlayerColour.GetColor(playerInfo.Value.ColorIndex);
        }
    }
}