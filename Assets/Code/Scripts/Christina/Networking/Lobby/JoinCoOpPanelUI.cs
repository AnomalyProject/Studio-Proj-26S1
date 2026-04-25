using System;
using UnityEngine;
using TMPro;
using UnityEditor.VersionControl;
using UnityEngine.UI;

public class JoinCoOpPanelUI : MonoBehaviour
{

    [SerializeField] private SteamFriendsLobbyBrowser friendsBrowser;

    [SerializeField] private Transform rowsParent;
    [SerializeField] private JoinableFriendRowUI rowPrefab;
    
    [SerializeField] private GameObject loadingState;
    [SerializeField] private GameObject listState;
    [SerializeField] private GameObject messageState;
    [SerializeField] private TMP_Text messageText;
    
    [SerializeField] private Button refreshButton;

    private void Awake()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(OnRefreshPressed);
        }
    }
    
    private void OnEnable()
    {
        if (friendsBrowser == null)
        {
            ShowMessage("Forgot friends browser reference!");
            return;
        }
        
        friendsBrowser.OnFriendsUpdated += HandleFriendsUpdated;
        friendsBrowser.OnRefreshFailed += HandleRefreshFailed;

        ClearRows();
        ShowLoading();
        friendsBrowser.RefreshJoinableFriends();
    }

    private void OnDisable()
    {
        if (friendsBrowser == null) return;

        friendsBrowser.OnFriendsUpdated -= HandleFriendsUpdated;
        friendsBrowser.OnRefreshFailed -= HandleRefreshFailed;
    }

  
    private void HandleFriendsUpdated()
    {
        ClearRows();

        var friends = friendsBrowser.JoinableFriends;

        if (friends.Count == 0)
        {
            ShowMessage("No Joinable friends are currently in this game.");
            return;
        }
        
        for (int i = 0; i < friends.Count; i++)
        {
            JoinableFriendRowUI row = Instantiate(rowPrefab, rowsParent);
            row.Setup(friends[i], HandleJoinRequested);
        }

        ShowList();
    }
    
    private void HandleJoinRequested(ulong lobbyId)
    {
        friendsBrowser.JoinFriendLobby(lobbyId);
    }

    private void HandleRefreshFailed(string error)
    {
        ClearRows();
        ShowMessage(error);
    }
    
    private void OnRefreshPressed()
    {
        ClearRows();
        friendsBrowser.RefreshJoinableFriends();
    }
    
    private void ShowLoading()
    {
        loadingState.SetActive(true);
        listState.SetActive(false);
        messageState.SetActive(false);
    }

    private void ShowList()
    {
        loadingState.SetActive(false);
        listState.SetActive(true);
        messageState.SetActive(false);
    }
    
    private void ShowMessage(string message)
    {
        loadingState.SetActive(false);
        listState.SetActive(false);
        messageState.SetActive(true);
        
        messageText.text = message;
    }
    
    private void ClearRows()
    {
        if (rowsParent == null) return;

        for (int i = rowsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(rowsParent.GetChild(i).gameObject);
        }
    }

}
