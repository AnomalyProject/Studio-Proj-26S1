using System.Collections;
using UnityEngine;

public class ElevatorButton : ExitInteractable
{
    public enum ButtonState
    {
        None,
        Unavailable,
        Available,
        Interacted
    }

    [SerializeField] private Renderer buttonRenderer;
    [SerializeField] Animation buttonAnimation;

    [Header("Colors")]
    [SerializeField] private Color unavailableColor = Color.gray;
    [SerializeField] private Color availableBright = Color.lightGreen;
    [SerializeField] private Color availableDark = Color.darkGreen;
    [SerializeField] private Color interactedColor = Color.lightGreen;

    [Header("Settings")]
    [SerializeField, Range(0,1)] private float fadeDuration = 0.25f;
    [SerializeField, Range(.1f,2)] private float blinkDuration = 0.75f;

    private Material materialInstance;
    private Coroutine currentRoutine;
    private ButtonState currentState = ButtonState.None;
    private bool afterInteraction = false;

    private void Awake()
    {
        materialInstance = buttonRenderer.material;
        exitPoint.OnPlayersChanged.AddListener(ChangeStateVisuals);
        exitPoint.OnActivateExit.AddListener((_) => DoInteractVisuals());
        exitPoint.OnAvailabilityChanged.AddListener((value) => ChangeStateVisuals(exitPoint.IsReadyToInteract));
    }

    private void ChangeStateVisuals(ButtonState state)
    {
        if (afterInteraction)
        {
            afterInteraction = false;
            return;
        }

        if (state == currentState) return;
        
        currentState = state;
        if (currentRoutine != null) StopCoroutine(currentRoutine);

        IEnumerator enumerator = null;

        switch (state)
        {
            case ButtonState.Unavailable:
                enumerator = FadeTo(unavailableColor);
                break;
            case ButtonState.Available:
                enumerator = BlinkRoutine();
                break;
            case ButtonState.Interacted:
                enumerator = FadeTo(interactedColor);
                break;
        }

        if(enumerator != null)
        currentRoutine = StartCoroutine(enumerator);
    }
    private void DoInteractVisuals()
    {
        ChangeStateVisuals(ButtonState.Interacted);
        afterInteraction = true;

        if(buttonAnimation)
        buttonAnimation.Play();
    }
    private void ChangeStateVisuals(bool available) => ChangeStateVisuals(available? ButtonState.Available : ButtonState.Unavailable);

    #region IEnumerators
    private IEnumerator FadeTo(Color target)
    {
        Color start = materialInstance.color;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;

            materialInstance.color = Color.Lerp(start, target, t);
            yield return null;
        }

        materialInstance.color = target;
    }
    private IEnumerator BlinkRoutine()
    {
        yield return FadeTo(availableDark);

        while (true)
        {
            yield return FadeBetween(availableDark, availableBright);
            yield return FadeBetween(availableBright, availableDark);
        }
    }
    private IEnumerator FadeBetween(Color fromColor, Color toColor)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / blinkDuration;
            materialInstance.color = Color.Lerp(fromColor, toColor, t);
            yield return null;
        }

        materialInstance.color = toColor;
    }
    #endregion
}