using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerInputLink : MonoBehaviour, IA_Global.IPlayerActions
{
    [SerializeField] PlayerBody playerBody;

    #region Input Events

    [SerializeField] UnityEvent<InputAction.CallbackContext> 
        onCrouch, onInteract, onLeanLeft, onLeanRight, 
        onLook, onMove, onNextItem, onPreviousItem, onScrollInventory,
        onSprint, onUseFocusedItem, onZoom;

    #endregion

    void Awake() => playerBody.QueueOnSpawned(SubscribeInput);

    private void SubscribeInput()
    {
        if (playerBody.isOwner) InputBridge.Player.SetCallbacks(this);
    }

    public void OnCrouch(InputAction.CallbackContext context) => onCrouch.Invoke(context);
    public void OnInteract(InputAction.CallbackContext context) => onInteract.Invoke(context);
    public void OnLeanLeft(InputAction.CallbackContext context) => onLeanLeft.Invoke(context);
    public void OnLeanRight(InputAction.CallbackContext context) => onLeanRight.Invoke(context);
    public void OnLook(InputAction.CallbackContext context) => onLook.Invoke(context);
    public void OnMove(InputAction.CallbackContext context) => onMove.Invoke(context);
    public void OnNextItem(InputAction.CallbackContext context) => onNextItem.Invoke(context);
    public void OnPreviousItem(InputAction.CallbackContext context) => onPreviousItem.Invoke(context);
    public void OnScrollInventory(InputAction.CallbackContext context) => onScrollInventory.Invoke(context);
    public void OnSprint(InputAction.CallbackContext context) => onSprint.Invoke(context);
    public void OnUseFocusedItem(InputAction.CallbackContext context) => onUseFocusedItem.Invoke(context);
    public void OnZoom(InputAction.CallbackContext context) => onZoom.Invoke(context);
}