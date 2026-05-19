using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UISelectableMenuItem : MonoBehaviour, IMenuSelectable
{
    [SerializeField] private Selectable selectable;
    private bool isSelected;

    public void Select()
    {
        isSelected = true;
        EventSystem.current.SetSelectedGameObject(
            selectable.gameObject);
    }

    public void Deselect()
    {
        isSelected = false;
        if (EventSystem.current.currentSelectedGameObject ==
            selectable.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        selectable.OnDeselect(null);
    }

    public void Submit()
    {
        // dododododod
    }
}