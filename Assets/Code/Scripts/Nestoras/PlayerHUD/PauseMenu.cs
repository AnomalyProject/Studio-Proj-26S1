using PurrNet;
using PurrNet.Transports;
using System.Collections;
using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Pause menu
/// </summary>
public class PauseMenu : MonoBehaviour
{
    private Transform root;
    private Coroutine mainMenuTransitionCoroutine;

    private void Awake() => root = transform.GetChild(0);
    private void OnEnable() => InputBridge.OnContextChanged += TogglePauseMenu;
    private void OnDisable() => InputBridge.OnContextChanged -= TogglePauseMenu;
    public void TogglePauseMenu(InputBridge.InputContext context)
    {
        // Disable when in Main Menu
        if (MainMenuManager.Instance != null)
        {
            root.gameObject.SetActive(false);
            return;
        }

        // Toggle
        root.gameObject.SetActive(context == InputBridge.InputContext.UI);

        // Make sure the settings page is disabled if Pause Menu is closed
        if (SettingsManager.IsOpen) SettingsManager.Close();
    }
    public void Resume() => InputBridge.SetContext(InputBridge.InputContext.Player);
    public void OpenSettings() => SettingsManager.Open();
    public void BackToMenu()
    {
        if (mainMenuTransitionCoroutine != null) return;
        mainMenuTransitionCoroutine = StartCoroutine(TransitionToMenu());
    }
    private IEnumerator TransitionToMenu()
    {
        BlackFadeManager.Instance?.FadeIn();
        yield return new WaitForSeconds(BlackFadeManager.Instance.TransitionTime);
        SessionModeManager.Instance.ReturnToMenu();
        mainMenuTransitionCoroutine = null;
    }
}
