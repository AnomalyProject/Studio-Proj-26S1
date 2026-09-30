using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// Navigate menus consisting of both 3D and UI Elements
public class MenuNavigation : MonoBehaviour
{

    [Header("Menu Items")]
    [SerializeField]
    private List<MonoBehaviour> menuItemBehaviours;
    private List<IMenuSelectable> menuItems = new List<IMenuSelectable>();

    private int currentIndex;

    private void Awake()
    {
        foreach (var behaviour in menuItemBehaviours)
        {
            if (behaviour is IMenuSelectable selectable)
            {
                menuItems.Add(selectable);
            }
        }
    }

    private void OnEnable()
    {
        if (InputBridge.CurrentContext != InputBridge.InputContext.UI)
        {
            Debug.LogWarning("Input is not UI and the menu navigation will not work.");
        }

        InputBridge.Actions.UI.Next.started += OnNext;
        InputBridge.Actions.UI.Previous.started += OnPrevious;
        InputBridge.Actions.UI.Submit.started += OnSubmit;

        RefreshSelection();
    }

    private void OnDisable()
    {
        InputBridge.Actions.UI.Next.started -= OnNext;
        InputBridge.Actions.UI.Previous.started -= OnPrevious;
        InputBridge.Actions.UI.Submit.started -= OnSubmit;
    }

    void OnNext(InputAction.CallbackContext ctx) => Navigate(1);
    void OnPrevious(InputAction.CallbackContext ctx) => Navigate(-1);

    private void Navigate(int delta)
    {
        menuItems[currentIndex].Deselect();
        currentIndex = (currentIndex + delta + menuItems.Count) % menuItems.Count;
        RefreshSelection();
    }

    private void OnSubmit(InputAction.CallbackContext ctx) => menuItems[currentIndex].Submit();
    private void RefreshSelection()
    {
        for (int i = 0; i < menuItems.Count; i++)
        {
            if (i == currentIndex)
            {
                menuItems[i].Select();
            }
            else
            {
                menuItems[i].Deselect();
            }
        }
    }

    public void Select(int index)
    {
        currentIndex = index;
        RefreshSelection();
    }
}