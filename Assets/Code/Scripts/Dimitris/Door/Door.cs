using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using PurrNet;
using UnityEngine;
/// <summary>
/// Door class that implements an interaction interface , basically open and close door
/// </summary>
[RequireComponent(typeof(Collider))]
public class Door : NetworkBehaviour, IInteractable<MonoBehaviour>
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
        if (isServer)
        {
            ToggleDoor();
        }
        else
        {
            RequestToggle_ServerRpc();
        }
        
        return Task.FromResult(true);
       
    }

    [ServerRpc]
    private void RequestToggle_ServerRpc()
    {
        ToggleDoor();
    }
    private void ToggleDoor()
    {
        if (isAnimating) return;

        isOpen = !isOpen;

        PlayAnimation(isOpen);

        DoorStateObservers(isOpen);
    }

    [ObserversRpc]
    private void DoorStateObservers(bool open)
    {
        isOpen = open;
        PlayAnimation(open);
    }
    // Handles playing the animation forward or backward
    private void PlayAnimation(bool open)
    {
        AnimationState state = anim[doorAnimationName];
        if (open)
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
        StartCoroutine(ResetAnimation(state.length));
        isOpen = !isOpen;//Door State change (Open or Closed = !isOpen)
    }
    // Coroutine that waits for the animation to finish
    private IEnumerator ResetAnimation(float duration)
    {
        yield return new WaitForSeconds(duration);
        isAnimating = false;
    }
}
