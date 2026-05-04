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
            if (CurrentContext == InputContext.Player) SetContext(InputContext.UI);
            else ToggleContext(CurrentContext);
        }
    }

    void IA_Global.IGlobalActions.OnToggleBug(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            if (CurrentContext == InputContext.DevConsole) return; // Don't allow toggling UI while in dev console
            ToggleContext(InputContext.BugReporter);
        }
    }
}