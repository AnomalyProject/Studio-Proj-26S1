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
        SteamRelayNetworkStatus_t status = new SteamRelayNetworkStatus_t();
        SteamNetworkingUtils.GetRelayNetworkStatus(out status);

        string statusText = $"Steam Relay: {status.m_eAvail}";

        string color = (status.m_eAvail == ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Current)? "green" : "red";

        GUI.Label(new Rect(10, Screen.height - 50, 300, 20), $"<color={color}>{statusText}</color>");
    }
}