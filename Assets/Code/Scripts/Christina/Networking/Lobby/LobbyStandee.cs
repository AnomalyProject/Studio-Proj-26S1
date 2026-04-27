using System;
using UnityEngine;
using TMPro;

public class LobbyStandee : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject hostBadge;
    [SerializeField] private Transform labelRoot;
    [SerializeField] private GameObject avatarRoot;

    [SerializeField] private Color readyColor = Color.green;
    [SerializeField] private Color notReadyColor = Color.red;

    private void LateUpdate()
    {
        if (labelRoot == null) return;
        
        Camera activeCamera = Camera.main;
        if (activeCamera == null) return;
        
        labelRoot.forward = activeCamera.transform.forward;
    }

    public void SetupOccupied(ClientPlayerInfo playerInfo)
    {
        avatarRoot.SetActive(true);
        labelRoot.gameObject.SetActive(true);
        
        nameText.text = playerInfo.DisplayName;
        statusText.text = playerInfo.IsReady ? "Ready" : "Not Ready";
        statusText.color = playerInfo.IsReady ? readyColor : notReadyColor;

        hostBadge.SetActive(playerInfo.IsHost);
    }
    
    public void SetupEmpty()
    {
        avatarRoot.SetActive(false);
        labelRoot.gameObject.SetActive(false);

        nameText.text = "";
        statusText.text = "";
        hostBadge.SetActive(false);
    }
}
