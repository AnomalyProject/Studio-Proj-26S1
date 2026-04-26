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

    public Task<bool> CanInteract(MonoBehaviour Interactor)
    {
        return Task.FromResult(!anim.isPlaying);  // Interaction is only allowed if not animating
    }
    // Attempts to interact with the door
    [ServerRpc] public Task<bool> TryInteract(MonoBehaviour Interactor) 
    {
        ToggleDoor_Server();
        return Task.FromResult(true);     
    }

    private void ToggleDoor_Server()
    {
        if (!isServer || anim.isPlaying) return;

        isOpen = !isOpen;
        PlayAnimation_Observers(isOpen);
    }

    [ObserversRpc] private void PlayAnimation_Observers(bool open)
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
    }
}
