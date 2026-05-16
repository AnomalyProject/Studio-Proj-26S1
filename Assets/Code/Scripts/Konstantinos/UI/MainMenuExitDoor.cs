using UnityEngine;
using UnityEngine.Events;

public class MainMenuExitDoor : MonoBehaviour
{
    public Animator DoorAnimator;
    public GameObject WarningIndicator;

    private bool Opened = false;

    public UnityEvent OnExitClicked;

    public float TimeToExit = 2f;
    private float exitTimer;


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
        DoorAnimator.SetBool("hover", true);
        WarningIndicator.SetActive(true);
    }

    private void OnMouseExit()
    {
        DoorAnimator.SetBool("hover", false);
        WarningIndicator.SetActive(false);
    }

    private void OnMouseUp()
    {
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
