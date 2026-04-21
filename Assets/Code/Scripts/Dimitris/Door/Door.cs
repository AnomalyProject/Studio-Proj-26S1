using System.Threading.Tasks;
using UnityEngine;
/// <summary>
/// Door class that implements an interaction interface , basically open and close door
/// </summary>
[RequireComponent(typeof(Collider))]
public class Door : MonoBehaviour, IInteractable<MonoBehaviour>
{
    [SerializeField] private Animation anim;
    [SerializeField] private string doorAnimationName = "Door Open";

    private bool isOpen = false;// Checks if door is currently open
    private bool isAnimating = false; // Prevents interaction while animation is playing

    public Task<bool> CanInteract(MonoBehaviour Interactor)
    {
        return Task.FromResult(!isAnimating);  // Interaction is only allowed if not animating
    }
    // Attempts to interact with the door
    public Task<bool> TryInteract(MonoBehaviour Interactor) 
    {
        Debug.Log("Door Interacted with by " + Interactor.name);
        PlayAnimation();//Animation PLay
        return Task.FromResult(true);
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
