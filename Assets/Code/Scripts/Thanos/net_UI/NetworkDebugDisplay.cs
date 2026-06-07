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

        int Xpos = 209;
        int Ypos = 173;

        SteamRelayNetworkStatus_t status = new SteamRelayNetworkStatus_t();
        SteamNetworkingUtils.GetRelayNetworkStatus(out status);

        string convertStatus = GetName(status.m_eAvail);

        string statusText = $"Steam Network Status: {convertStatus}";

        string color = (status.m_eAvail == ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Current)? "green" : "red";

        GUI.Label(new Rect(Screen.width - Xpos, Ypos, 400, 20), $"<color={color}>{statusText}</color>");
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
