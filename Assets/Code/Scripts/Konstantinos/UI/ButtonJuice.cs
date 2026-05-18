using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    // local variables for caching
    private Button myButton;
    private Vector3 originalScale;
    private Vector3 hoverScale;
    private bool isHovering = false;

    [Header("General Settings")] // customizable through the inspector
    [SerializeField] float hoverScaleFactor = 1.2f; // how much the btnn grows
    [SerializeField] float squishFactor = 0.8f; // how much it shrinks when pressed
    [SerializeField] float scaleSpeed = 0.1f; // how quickly it grows
    [SerializeField] float squishSpeed = 0.05f; // how quickly it shrinks


    [Space(10)]
    [Header("Sound Effects")]
    // sound effects for more impact (aka JUICE!!!)
    public AudioClip hoverSound, exitSound, clickSound, releaseSound;
    private AudioSource audioSource;

    void Awake()
    {
        // cache local for later use
        originalScale = transform.localScale;
        hoverScale = originalScale * hoverScaleFactor;

        audioSource = GetComponent<AudioSource>();
        myButton = GetComponent<Button>();
    }

    private bool IsInteractable()
    {
        return myButton != null && myButton.interactable;
    }

    private void OnEnable()
    {
        // reset scale when the player changes menus
        transform.localScale = originalScale;
    }

    // when the mouse is on top of a button
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable()) return;

        PlaySound(hoverSound);
        isHovering = true;
        if (GetComponent<UISelectableMenuItem>() == null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
        else
        {
            GetComponent<UISelectableMenuItem>().Select();
        }
        StopAllCoroutines();
        StartCoroutine(ScaleButton(transform.localScale, hoverScale));
    }

    // when the mouse leaves a button
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!IsInteractable()) return;

        PlaySound(exitSound);
        isHovering = false;
        StopAllCoroutines();
        StartCoroutine(ScaleButton(transform.localScale, originalScale));
    }

    // when mouse clicks but holds the button
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable()) return;

        PlaySound(clickSound);
        StopAllCoroutines();
        StartCoroutine(SquishButton(transform.localScale, originalScale * squishFactor));
    }

    // when mouse clicks and releases a button
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsInteractable()) return;

        PlaySound(releaseSound);
        StopAllCoroutines();
        StartCoroutine(ScaleButton(transform.localScale, isHovering ? hoverScale : originalScale));
    }

    // apply hover for keyboard and controller navigation
    public void OnSelect(BaseEventData eventData)
    {
        if (!IsInteractable()) return;

        isHovering = true;
        StopAllCoroutines();
        StartCoroutine(ScaleButton(transform.localScale, hoverScale));

        PlaySound(hoverSound); 
    }

    // reset when keyboard/controller selects a different button
    public void OnDeselect(BaseEventData eventData)
    {
        if (!IsInteractable()) return;

        isHovering = false;
        StopAllCoroutines();
        StartCoroutine(ScaleButton(transform.localScale, originalScale));

        PlaySound(exitSound); 
    }

    private void PlaySound(AudioClip clip)
    {
        // PlayOneShot does not cancel previous audio plays
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
    }

    private IEnumerator ScaleButton(Vector3 startScale, Vector3 endScale)
    {
        float timeElapsed = 0;
        while (timeElapsed < scaleSpeed)
        {
            // smooth scale between start and end scale
            transform.localScale = Vector3.Lerp(startScale, endScale, timeElapsed / scaleSpeed);
            timeElapsed += Time.unscaledDeltaTime; // increase even when TimeScale is 0
            yield return null;
        }
        transform.localScale = endScale; // final confirm
    }

    private IEnumerator SquishButton(Vector3 startScale, Vector3 endScale)
    {
        float timeElapsed = 0;
        while (timeElapsed < squishSpeed)
        {
            // smooth scale between start and end scale
            transform.localScale = Vector3.Lerp(startScale, endScale, timeElapsed / squishSpeed);
            timeElapsed += Time.unscaledDeltaTime;  // increase even when TimeScale is 0
            yield return null;
        }
        transform.localScale = endScale; // final confirm
    }
}
