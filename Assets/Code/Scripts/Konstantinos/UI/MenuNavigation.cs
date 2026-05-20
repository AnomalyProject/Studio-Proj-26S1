using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// Navigate menus consisting of both 3D and UI Elements
public class MenuNavigation : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset actionsAsset;
    [SerializeField] private string navigateActionName = "Navigate"; 
    [SerializeField] private string submitActionName = "Submit";

    [Header("Menu Items")]
    [SerializeField]
    private List<MonoBehaviour> menuItemBehaviours;

    private List<IMenuSelectable> menuItems =
        new List<IMenuSelectable>();

    private InputAction navigateAction;
    private InputAction submitAction;

    private int currentIndex;

    private void Awake()
    {
        navigateAction =
            actionsAsset.FindAction(navigateActionName);

        submitAction =
            actionsAsset.FindAction(submitActionName);

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
        navigateAction.performed += OnNavigate;
        submitAction.performed += OnSubmit;

        RefreshSelection();
    }

    private void OnDisable()
    {
        navigateAction.performed -= OnNavigate;
        submitAction.performed -= OnSubmit;
    }

    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        Vector2 move = ctx.ReadValue<Vector2>();

        if (Mathf.Abs(move.y) < 0.5f &&
            Mathf.Abs(move.x) < 0.5f)
            return;

        menuItems[currentIndex].Deselect();

        currentIndex++;

        if (currentIndex >= menuItems.Count)
            currentIndex = 0;

        RefreshSelection();
    }

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        menuItems[currentIndex].Submit();
    }

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