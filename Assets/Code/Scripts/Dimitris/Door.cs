using UnityEngine;
/// <summary>
/// Door class that implements an interaction interface , basically open and close door
/// </summary>
[RequireComponent(typeof(Collider))]
public class Door : MonoBehaviour, IInteractable<FPSController>
{
    [SerializeField] private Animation anim;
    [SerializeField] private string doorAnimationName = "Door Open";

    private bool isOpen = false;// Checks if door is currently open
    private bool isAnimating = false; // Prevents interaction while animation is playing

    public bool CanInteract(FPSController Interactor)
    {
        return !isAnimating;  // Interaction is only allowed if not animating
    }
    // Attempts to interact with the door
    public bool TryInteract(FPSController Interactor) 
    {
        if(!CanInteract(Interactor))
        {
            return false; 
        }
        Debug.Log("Door Interacted with by " + Interactor.name);
        PlayAnimation();//Animation PLay
        return true;
    }
    // Handles playing the animation forward or backward
    private void PlayAnimation()
    {
        AnimationState state = anim[doorAnimationName];
        if (!isOpen)
        {
            state.speed = 1f;
            state.time = 0f;
        }
        else
        {
            state.speed = -1f;
            state.time = state.length;
        }
        anim.Play(doorAnimationName);
        isAnimating = true;
        StartCoroutine(WaitForAnimation(state.length));
        isOpen = !isOpen;//Door State change (Open or Closed = !isOpen)
    }
    // Coroutine that waits for the animation to finish
    private System.Collections.IEnumerator WaitForAnimation(float duration)
    {
        yield return new WaitForSeconds(duration);
        isAnimating = false;
    }
}
