using UnityEngine;
using UnityEngine.InputSystem;
/// <summary>
///  This script allows the player to interact with a Door object
/// </summary>
public class InteractTest : MonoBehaviour
{
    public Door door;// Reference to a Door

    void Update()
    {
        // Check if the "E" key was pressed during this frame and then try to interact
        if (Keyboard.current.eKey.wasPressedThisFrame) 
        {
            door.TryInteract(this);
        }
    }
}