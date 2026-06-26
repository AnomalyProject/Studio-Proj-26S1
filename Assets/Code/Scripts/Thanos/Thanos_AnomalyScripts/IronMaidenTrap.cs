using UnityEngine;
using System.Collections;

/// <summary>
/// Represents a trap that simulates an Iron Maiden, automatically trapping and releasing the player when they enter its
/// trigger area.
/// </summary>
/// <remarks>Attach this component to a GameObject with a trigger collider to enable the Iron Maiden trap
/// behavior. When a player enters the trigger, the trap closes, holds the player for a specified duration, and then
/// releases them. The trap requires references to an Animator for door control and a BoxCollider for managing the
/// door's physical state. This component is intended for use in Unity scenes and should be configured via the
/// Inspector.</remarks>
public class IronMaidenTrap : MonoBehaviour
{
    [Header("Attributes")]

    [SerializeField] private Animator ironMaidenAnimator;
    [SerializeField] private float trapDuration = 5f;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioSource audioSource;

    private Coroutine trapCoroutine;
    private bool isPlayerInside = false;

    public void InvokeTrigger()
    {     
        if (trapCoroutine != null)
        {
            return;
        }

        isPlayerInside = true;
        CloseDoors();
        trapCoroutine = StartCoroutine(OpenDoors());
    }

    public void CloseDoors()
    {
        ironMaidenAnimator.SetBool("IsOpen", false);
        audioSource.PlayOneShot(closeSound);
        Debug.Log("Player got trapped inside the Iron Maiden!");
    }

    public IEnumerator OpenDoors()
    {
        yield return new WaitForSeconds(trapDuration);

        ironMaidenAnimator.SetBool("IsOpen", true);
        audioSource.PlayOneShot(openSound);
        Debug.Log("Player is free to leave the Iron Maiden!");
        isPlayerInside = false;
        trapCoroutine = null;
    }
}