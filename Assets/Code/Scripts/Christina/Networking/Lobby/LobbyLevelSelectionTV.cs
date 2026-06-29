using UnityEngine;
using PurrNet;
using TMPro;
using UnityEngine.UI;

public class LobbyLevelSelectionTV : NetworkBehaviour
{
    [SerializeField] private LevelCatalog levelCatalog;

    [Header("Screen")]
    [SerializeField] private Image previewImage;
    [SerializeField] private TMP_Text levelNameText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text selectedText;
    
     private SyncVar<int> focusedIndex = new(0, ownerAuth: false);
    
     protected override void OnSpawned(bool asServer)
     {
         base.OnSpawned(asServer);

         focusedIndex.onChanged += HandleFocusedIndexChanged;
         RefreshScreen();
     }

     protected override void OnDespawned()
     {
         focusedIndex.onChanged -=HandleFocusedIndexChanged;
         base.OnDespawned();
     }
     
     private void OnEnable()
     {
         SessionEvents.OnSessionDataChanged += RefreshScreen;
     }

     private void OnDisable()
     {
         SessionEvents.OnSessionDataChanged -= RefreshScreen;
     }
     
     [ServerRpc(requireOwnership: false)]
     public void RequestNextLevel(RPCInfo info = default)
     {
         if (!WasRequestedByHost(info)) return;
         if (!HasLevels()) return;

         int newIndex = focusedIndex.value + 1;

         if (newIndex >= levelCatalog.Levels.Count) newIndex = 0;

         focusedIndex.value = newIndex;
     }
     
     [ServerRpc(requireOwnership: false)]
     public void RequestPreviousLevel(RPCInfo info = default)
     {
         if (!WasRequestedByHost(info)) return;
         if (!HasLevels()) return;

         int newIndex = focusedIndex.value - 1;

         if (newIndex < 0) newIndex = levelCatalog.Levels.Count - 1;

         focusedIndex.value = newIndex;
     }
     
     [ServerRpc(requireOwnership: false)]
     public void RequestSelectFocusedLevel(RPCInfo info = default)
     {
         if (!HasLevels()) return;
         if (SessionManager.Instance == null) return;
         if (!levelCatalog.TryGetLevel(focusedIndex.value, out LevelDefinition level)) return;

         SessionManager.Instance.RequestSelectLevel(level.Id, info);
     }
     
     private void RefreshScreen()
     {
         if (!HasLevels()) return;
         if (!levelCatalog.TryGetLevel(focusedIndex.value, out LevelDefinition level)) return;

         if (levelNameText != null) levelNameText.text = level.DisplayName;
         if (progressText != null) progressText.text = $"{focusedIndex.value + 1}/{levelCatalog.LevelCount}";

         if (previewImage != null)
         {
             previewImage.sprite = level.Preview;
             previewImage.enabled = level.Preview != null;
         }

         if (selectedText != null)
         {
             selectedText.text = IsCurrentlySelected(level) ? "Selected" : "Select";
         }
     }
     
     public bool CanLocalPlayerControl()
     {
         return SessionManager.Instance != null && SessionManager.Instance.IsHost;
     }
     
     //helpers
     private void HandleFocusedIndexChanged(int _)
     {
         RefreshScreen();
     }

     private bool WasRequestedByHost(RPCInfo info)
     {
         return SessionManager.Instance != null && SessionManager.Instance.IsPlayerHost(info.sender);
     }
     
     private bool IsCurrentlySelected(LevelDefinition level)
     {
         if (SessionManager.Instance == null || level == null) return false;
         
         if (SessionManager.Instance.CurrentSession != null)
         {
             return SessionManager.Instance.CurrentSession.SelectedLevelId == level.Id;
         }

         return SessionManager.Instance.LatestClientSession.SelectedLevelId == level.Id;
     }

     private bool HasLevels()
     {
         return levelCatalog != null && levelCatalog.Levels != null && levelCatalog.Levels.Count > 0;
     }
}
