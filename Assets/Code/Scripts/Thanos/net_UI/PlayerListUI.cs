using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class PlayerListUI : MonoBehaviour
{
    [Header("Player List UI")]
    [Space(2)]
    [Tooltip("Player Name displayText")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("Ready, Not Ready displayText")]
    [SerializeField] private TMP_Text statusText;
    [Tooltip("is Host badge")]
    [SerializeField] private GameObject hostIndicator;
    
    [SerializeField] private Button kickButton;
    private ulong playerSteamID;

    public void Setup(ClientPlayerInfo playerInfo, ulong localSteamID, Action<ulong> onKickClicked,bool canStartKickVote)
    {
        playerSteamID = playerInfo.SteamID;
        
        nameText.text = playerInfo.DisplayName;
        hostIndicator.SetActive(playerInfo.IsHost);
        
        bool canKick = canStartKickVote && playerInfo.SteamID != localSteamID && !playerInfo.IsHost && !playerInfo.IsWaitingToReconnect;
        
        if (kickButton != null)
        {
            kickButton.gameObject.SetActive(canKick);
            kickButton.onClick.RemoveAllListeners();

            if (canKick)
            {
                kickButton.onClick.AddListener(() => onKickClicked?.Invoke(playerSteamID));
            }
        }
        
        // disconnection check
        if (playerInfo.IsWaitingToReconnect)
        {
            statusText.text = "Disconnected - waiting to reconnect";
            statusText.color = Color.yellow;
            hostIndicator.SetActive(playerInfo.IsHost);
            return;
        }
        
        nameText.text = playerInfo.DisplayName;
        
        //Ready status colour
        statusText.text = playerInfo.IsReady ? "Ready" : "Not Ready";
        statusText.color = playerInfo.IsReady ? Color.green : Color.red;
        
        //Show/hide Host icon
        hostIndicator.SetActive(playerInfo.IsHost);
        
        // adding indicators for ready/ in elevator state
        if (playerInfo.IsReady && playerInfo.IsInElevator)
        {
            statusText.text = "Ready - In Elevator";
            statusText.color = Color.green;
        }
        else if (playerInfo.IsInElevator)
        {
            statusText.text = "In Elevator";
            statusText.color = Color.yellow;
        }
        else
        {
            statusText.text = "Not Ready";
            statusText.color = Color.red;
        }
    }
}
