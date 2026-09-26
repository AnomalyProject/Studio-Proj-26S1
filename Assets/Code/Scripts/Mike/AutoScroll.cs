using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Scrollbar))]
public class AutoScroll : MonoBehaviour
{
    [SerializeField] float autoSnapOffset = 0.15f;
    Scrollbar scroll;

    private void Awake() => scroll = GetComponent<Scrollbar>();
    private void OnEnable() => InputBridge.Actions.UI.Navigate.performed += UpdateValue;
    private void OnDisable() => InputBridge.Actions.UI.Navigate.performed -= UpdateValue;
    void UpdateValue(InputAction.CallbackContext ctx)
    {
        if (ctx.control.device is not Gamepad) return;

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        float value = 1 - (float)currentSelected.transform.GetSiblingIndex() / (currentSelected.transform.parent.childCount - 1);

        if (value <= autoSnapOffset) value = 0;
        else if (value >= 1 - autoSnapOffset) value = 1;

        scroll.value =  value;
    }
}