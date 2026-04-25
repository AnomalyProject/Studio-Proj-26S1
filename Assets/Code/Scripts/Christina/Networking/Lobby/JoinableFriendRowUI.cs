using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class JoinableFriendRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text displayNameText;
    [SerializeField] private Button joinButton;

    private ulong lobbyID;
    private Action<ulong> onJoinPressed;

    public void Setup(JoinableFriendInfo friendInfo, Action<ulong> joinCallback)
    {
        lobbyID = friendInfo.LobbyID;
        onJoinPressed = joinCallback;
        
        displayNameText.text = friendInfo.DisplayName;
        
        joinButton.onClick.RemoveListener(HandleJoinPressed);
        joinButton.onClick.AddListener(HandleJoinPressed);
    }

    private void OnDisable()
    {
        if (joinButton != null)
        {
            joinButton.onClick.RemoveListener(HandleJoinPressed);
        }
    }

    private void HandleJoinPressed()
    {
        onJoinPressed?.Invoke(lobbyID);
    }
}
