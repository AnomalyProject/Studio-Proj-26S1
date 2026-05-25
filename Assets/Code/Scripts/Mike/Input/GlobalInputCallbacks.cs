using UnityEngine.InputSystem;
using static InputBridge;

public class GlobalInputCallbacks : IA_Global.IGlobalActions
{
    void IA_Global.IGlobalActions.OnToggleDev(InputAction.CallbackContext ctx)
    {
        if (ctx.started) ToggleContext(InputContext.DevConsole);
    }

    void IA_Global.IGlobalActions.OnToggleUI(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            if (CurrentContext == InputContext.Player || CurrentContext == InputContext.noClip) SetContext(InputContext.UI);
            else ToggleContext(CurrentContext);
        }
    }

    void IA_Global.IGlobalActions.OnToggleBug(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            // Don't allow toggling bug reporter from the dev console
            if (CurrentContext == InputContext.DevConsole) return;
            ToggleContext(InputContext.BugReporter);
        }
    }

    public void OnToggleChat(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            // Don't allow opening chat unless we're in the player context
            if (CurrentContext != InputContext.Player) return;
            if (CurrentContext != InputContext.Chat) SetContext(InputContext.Chat);
        }
    }
}