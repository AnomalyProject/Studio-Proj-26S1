using UnityEngine;
using Steamworks;

public class NetworkDebugDisplay : MonoBehaviour
{
    [SerializeField] private PurrNet.StatisticsManager statsManager;

    private bool _showOverlay = true;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Comma))
        {
            _showOverlay = !_showOverlay;
            statsManager.enabled = _showOverlay;
        }
    }

    private void OnGUI()
    {
        if (!_showOverlay || !statsManager.enabled) return;

        DrawSteamDiagnostic();
    }

    private void DrawSteamDiagnostic()
    {
        // #if UNITY_EDITOR 
        // return;
        // #endif
        
        // -- from christina: added guard so the console doesn't scream if steam isn't available
        if (!SteamManager.Initialized) return;

        SteamRelayNetworkStatus_t status = new SteamRelayNetworkStatus_t();
        SteamNetworkingUtils.GetRelayNetworkStatus(out status);

        string convertStatus = GetName(status.m_eAvail);

        string statusText = $"Steam Network Status: {convertStatus}";

        string color = (status.m_eAvail == ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Current)? "green" : "red";
        
        int width = 400;
        int height = 20;
        int rightPadding = 12;
        int bottomPadding = 165; // higher number, higher it gets
        
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.richText = true;
        style.alignment = TextAnchor.UpperRight;

        GUI.Label(new Rect(Screen.width - width - rightPadding, Screen.height - bottomPadding, width, height),$"<color={color}>{statusText}</color>", style);
        
    }

    private string GetName(ESteamNetworkingAvailability status)
    {
        switch (status)
        {
            case ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Current:
                return "Connected";
            case ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Attempting:
                return "Connecting...";
            case ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Retrying:
                return "Retrying Connection...";
            case ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_NeverTried:
                return "Not Initialized";
            case ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Failed:
            case ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_CannotTry:
                return "Disconnected";
            case ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Unknown:
            default:
                return "Unknown Status";
        }
    }
}
