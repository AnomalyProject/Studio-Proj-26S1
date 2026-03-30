using UnityEngine;

/// <summary>
/// Hides the player's visual body for the local owner (industry standard practice, prevents camera clipping),
/// keeps it visible for all other players.
/// </summary>
public class PlayerVisibility : MonoBehaviour
{
    [SerializeField] private GameObject bodyVisuals;
    private FPSInputHandler inputHandler;

    private void Awake()
    {
        inputHandler = GetComponentInParent<FPSInputHandler>();
    }
    
    void Start()
    {
        if (inputHandler == null) return;

        // owner: hide body so it doesn't clip the camera
        // other clients: show body so they can see this player
        bodyVisuals.SetActive(!inputHandler.isOwner);
    }

}
