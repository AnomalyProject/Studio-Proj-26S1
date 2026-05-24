using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Pause menu
/// </summary>
public class PauseMenu : MonoBehaviour
{
    private Transform root;
    private void Awake() => root = transform.GetChild(0);
    private void OnEnable() => InputBridge.OnContextChanged += TogglePauseMenu;
    private void OnDisable() => InputBridge.OnContextChanged -= TogglePauseMenu;
    public void TogglePauseMenu(InputBridge.InputContext context)
    {
        if (MainMenuManager.instance == null) root.gameObject.SetActive(context == InputBridge.InputContext.UI);
        else root.gameObject.SetActive(false);
    }
    public void Resume() => InputBridge.SetContext(InputBridge.InputContext.Player);
    public void OpenSettings() => SettingsManager.Open();
    public void BackToMenu() => SessionModeManager.Instance.ReturnToMenu();
    public void QuitGame() => DevConsole.commands["exit"].Execute(null);
}
