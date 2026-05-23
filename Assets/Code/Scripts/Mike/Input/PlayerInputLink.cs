using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine;

public class PlayerInputLink : MonoBehaviour, IA_Global.IPlayerActions
{
    [SerializeField] PlayerBody playerBody;

    #region Input Events

    [SerializeField] UnityEvent<InputAction.CallbackContext> 
        onCrouch, onInteract, onLeanLeft, onLeanRight, 
        onLook, onMove, onNextItem, onPreviousItem, onScrollInventory,
        onSprint, onUseFocusedItem, onZoom, onDropItem, onPingLocation, onShove;

    #endregion

    void Awake()
    {
        if (playerBody.isSpawned) SubscribeInput();
        else playerBody.QueueOnSpawned(SubscribeInput);
    }

    private void SubscribeInput()
    {
        if (playerBody.isOwner)
        {
            InputBridge.Actions.Player.SetCallbacks(this);
            InputBridge.SetContext(InputBridge.InputContext.Player);
        }
    }

    void IA_Global.IPlayerActions.OnCrouch(InputAction.CallbackContext context) => onCrouch.Invoke(context);
    void IA_Global.IPlayerActions.OnInteract(InputAction.CallbackContext context) => onInteract.Invoke(context);
    void IA_Global.IPlayerActions.OnLeanLeft(InputAction.CallbackContext context) => onLeanLeft.Invoke(context);
    void IA_Global.IPlayerActions.OnLeanRight(InputAction.CallbackContext context) => onLeanRight.Invoke(context);
    void IA_Global.IPlayerActions.OnLook(InputAction.CallbackContext context) => onLook.Invoke(context);
    void IA_Global.IPlayerActions.OnMove(InputAction.CallbackContext context) => onMove.Invoke(context);
    void IA_Global.IPlayerActions.OnNextItem(InputAction.CallbackContext context) => onNextItem.Invoke(context);
    void IA_Global.IPlayerActions.OnPreviousItem(InputAction.CallbackContext context) => onPreviousItem.Invoke(context);
    void IA_Global.IPlayerActions.OnScrollInventory(InputAction.CallbackContext context) => onScrollInventory.Invoke(context);
    void IA_Global.IPlayerActions.OnSprint(InputAction.CallbackContext context) => onSprint.Invoke(context);
    void IA_Global.IPlayerActions.OnUseFocusedItem(InputAction.CallbackContext context) => onUseFocusedItem.Invoke(context);
    void IA_Global.IPlayerActions.OnZoom(InputAction.CallbackContext context) => onZoom.Invoke(context);
    void IA_Global.IPlayerActions.OnDropItem(InputAction.CallbackContext context) => onDropItem.Invoke(context);
    void IA_Global.IPlayerActions.OnPingLocation(InputAction.CallbackContext context) => onPingLocation.Invoke(context);
    void IA_Global.IPlayerActions.OnShove(InputAction.CallbackContext context) => onShove.Invoke(context);
}