using UnityEngine.InputSystem;
using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Pause menu that toggles the player's cursor and input.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    private IA_UserInterface UIInputActions;
    private PlayerInput playerInput;
    private Transform root;

    private bool isPaused;

    private void Awake()
    {
        UIInputActions = new IA_UserInterface();
        root = transform.GetChild(0);

        if (PlayerBody.localPlayerBody != null) HookToLocalPlayer(PlayerBody.localPlayerBody);
        PlayerBody.OnLocalPlayerSpawned += HookToLocalPlayer;
        PlayerBody.OnLocalPlayerDespawned += HandleLocalPlayerDespawned;

    }
    private void OnEnable()
    {
        UIInputActions.Enable();
        UIInputActions.UI.TogglePauseMenu.performed += TogglePauseMenu;
    }
    private void OnDisable()
    {
        UIInputActions.Disable();
        UIInputActions.UI.TogglePauseMenu.performed -= TogglePauseMenu;
    }

    private void HookToLocalPlayer(PlayerBody player)
    {
        playerInput = player.GetComponent<PlayerInput>();
        if (isPaused) playerInput.DeactivateInput();
    }
    private void HandleLocalPlayerDespawned(PlayerBody player) => playerInput = null;

    public void TogglePauseMenu(InputAction.CallbackContext context) => TogglePauseMenu();
    public void TogglePauseMenu()
    {
        isPaused = !isPaused;
        root.gameObject.SetActive(isPaused);

        if (isPaused)
        {
            if (playerInput != null) playerInput.DeactivateInput();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (playerInput != null) playerInput.ActivateInput();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    public void QuitGame() => DevConsole.commands["exit"].Execute(null);
}
