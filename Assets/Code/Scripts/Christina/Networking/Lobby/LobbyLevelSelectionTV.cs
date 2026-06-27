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

         focusedIndex.onChanged += RefreshScreen;
         RefreshScreen(focusedIndex.value);
     }

     protected override void OnDespawned()
     {
         focusedIndex.onChanged -= RefreshScreen;
         base.OnDespawned();
     }
     
     [ServerRpc(requireOwnership: false)]
     public void RequestNextLevel()
     {
         if (!HasLevels()) return;

         int newIndex = focusedIndex.value + 1;

         if (newIndex >= levelCatalog.Levels.Count) newIndex = 0;

         focusedIndex.value = newIndex;
     }
     
     [ServerRpc(requireOwnership: false)]
     public void RequestPreviousLevel()
     {
         if (!HasLevels()) return;

         int newIndex = focusedIndex.value - 1;

         if (newIndex < 0) newIndex = levelCatalog.Levels.Count - 1;

         focusedIndex.value = newIndex;
     }
     
     [ServerRpc(requireOwnership: false)]
     public void RequestSelectFocusedLevel()
     {
         if (!HasLevels()) return;
         if (SessionManager.Instance == null) return;

         LevelDefinition level = levelCatalog.Levels[focusedIndex.value];
         SessionManager.Instance.RequestSelectLevel(level.Id);
     }
     
     private void RefreshScreen(int index)
     {
         if (!HasLevels()) return;

         LevelDefinition level = levelCatalog.Levels[index];

         if (levelNameText != null) levelNameText.text = level.DisplayName;

         if (previewImage != null) previewImage.sprite = level.Preview;

         if (progressText != null) progressText.text = "Progress: 0%";

         if (selectedText != null) selectedText.text = IsCurrentlySelected(level) ? "Selected" : "Preview";
     }
     
     //helpers
     private bool IsCurrentlySelected(LevelDefinition level)
     {
         if (SessionManager.Instance == null) return false;
         if (SessionManager.Instance.CurrentSession == null) return false;

         return SessionManager.Instance.CurrentSession.SelectedLevelId == level.Id;
     }

     private bool HasLevels()
     {
         return levelCatalog != null && levelCatalog.Levels != null && levelCatalog.Levels.Count > 0;
     }
}
