using UnityEngine;
using UnityEngine.InputSystem;
using System;
public class FlashLightTester : MonoBehaviour
{
    [SerializeField] private Flashlight flashlight;

    private PlayerInputs input;

    private void Awake()
    {
        input = new PlayerInputs();
        input.Enable();
    }

    private void Update()
    {
        if (input.Player.ToggleFlashlight.triggered)
        {
            _ = flashlight.TryInteract(this);
        }

        if (input.Player.FullRecharge.triggered)
        {
            flashlight.FullRecharge();
            Debug.Log("Refilled!");
        }
    }
}
