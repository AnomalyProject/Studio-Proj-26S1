using UnityEngine.InputSystem;
using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Pause menu that also toggles the player's input context.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    private InputAction togglePauseAction;
    private Transform root;

    private bool isPaused;

    private void Awake()
    {
        togglePauseAction = InputBridge.Actions.FindAction("Toggle UI");
        root = transform.GetChild(0);
    }
    private void OnEnable() => togglePauseAction.performed += TogglePauseMenu;
    private void OnDisable() => togglePauseAction.performed -= TogglePauseMenu;
    public void TogglePauseMenu(InputAction.CallbackContext context)
    {
        if (InputBridge.CurrentContext != InputBridge.InputContext.Player && InputBridge.CurrentContext != InputBridge.InputContext.UI) return;
        isPaused = !isPaused;
        root.gameObject.SetActive(isPaused);
    }
    public void Resume()
    {
        if (InputBridge.CurrentContext != InputBridge.InputContext.UI) return;
        InputBridge.SetContext(InputBridge.InputContext.Player);
        TogglePauseMenu(new InputAction.CallbackContext());
    }

    public void BackToMenu() => SessionModeManager.Instance.ReturnToMenu();
    public void QuitGame() => DevConsole.commands["exit"].Execute(null);
}
