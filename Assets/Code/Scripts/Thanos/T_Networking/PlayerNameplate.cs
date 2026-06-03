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
        nameText.color = PlayerColour.GetColor(colourIndex);
    }
    
    
    // chose late update to avoid jittering
    private void LateUpdate()
    {
        if (nameplateVisuals == null || !nameplateVisuals.activeSelf) return;
        
        if (PlayerBody.localPlayerBody?.CameraController == null) return;

        // rotate nameplate to face the local player's camera
        transform.LookAt(PlayerBody.localPlayerBody.CameraController.transform);
    }
    
    protected override void OnSpawned(bool asServer) 
    {
        base.OnSpawned(asServer);

        if (!SteamManager.Initialized) return;
        if (!owner.HasValue) return;

        ulong ownerSteamID = (ulong)owner.Value.id;
        
        SessionData currentSession = SessionManager.Instance?.CurrentSession;
        PlayerSessionInfo? playerInfo = currentSession?.GetPlayer(ownerSteamID);

        if (playerInfo.HasValue)
        {
            nameText.text = playerInfo.Value.DisplayName;
            nameText.color = playerInfo.Value.GetPlayerColor();
        }
    }
}