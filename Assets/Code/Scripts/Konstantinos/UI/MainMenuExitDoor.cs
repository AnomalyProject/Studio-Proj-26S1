using UnityEngine;
using UnityEngine.Events;

public class MainMenuExitDoor : MonoBehaviour, IMenuSelectable
{
    public MenuNavigation MenuNavigation;
    public Animator DoorAnimator;
    public GameObject WarningIndicator;

    private bool Opened = false;

    public UnityEvent OnExitClicked;

    public float TimeToExit = 2f;
    private float exitTimer;

    private bool isSelected;


    private void Update()
    {
        if (Opened)
        {
            exitTimer -= Time.deltaTime;
            if (exitTimer <= 0)
            {
                Opened = false;
                MainMenuManager.instance.QuitGame();
            }
        }
    }


    private void OnMouseEnter()
    {
        if (MenuNavigation != null)
        {
            MenuNavigation.Select(1);
        }
        else
        {
            Select();
        }
    }

    private void OnMouseExit()
    {
        Deselect();
    }

    private void OnMouseUp()
    {
        Submit();
    }

    public void Select()
    {
        Debug.Log("Door Selected");
        isSelected = true;

        // Highlight door
        DoorAnimator.SetBool("hover", true);
        WarningIndicator.SetActive(true);
    }

    public void Deselect()
    {
        Debug.Log("Door Deselected");
        isSelected = false;

        // Remove highlight
        DoorAnimator.SetBool("hover", false);
        WarningIndicator.SetActive(false);
    }

    public void Submit()
    {
        Debug.Log("Door Activated");

        // Open door
        if (!Opened && enabled)
        {
            exitTimer = TimeToExit;
            Opened = true;
            DoorAnimator.SetTrigger("Open");
            WarningIndicator.SetActive(false);
            OnExitClicked?.Invoke();
        }
    }
}
