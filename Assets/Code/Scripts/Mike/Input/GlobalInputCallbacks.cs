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
            if (CurrentContext == InputContext.UI) SetContext(InputContext.Player);
            else SetContext(InputContext.UI);
        }
    }
}