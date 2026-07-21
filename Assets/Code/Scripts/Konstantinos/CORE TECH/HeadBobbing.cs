using UnityEngine;
using UnityEngine.InputSystem;

public class HeadBobbing : MonoBehaviour
{
    [SerializeField] private FPSController controller;

    [Header("Smoothing")]
    [SerializeField, Tooltip("Position smoothing speed.")] private float positionSmooth = 15f;
    [SerializeField, Tooltip("Rotation smoothing speed.")] private float rotationSmooth = 15f;


    [Header("Idle Breathing")]
    [SerializeField, Tooltip("Breathing speed.")] private float idleFrequency = 1.5f;
    [SerializeField, Tooltip("Breathing amplitude.")] private float idleAmplitude = 0.004f;


    [Header("Bobbing")]
    [SerializeField, Tooltip("Movement bobbing speed.")] private float bobFrequency = 8f;
    [SerializeField, Min(1), Tooltip("Bobbing multiplier applied while sprinting.")] private float sprintMultiplier = 1.5f;
    [SerializeField, Range(.1f, 1), Tooltip("Bobbing multiplier applied while crouching.")] private float crouchedMultiplier = 0.5f;
    [SerializeField, Min(0), Tooltip("Overall bob intensity.")] private float bobAmplitude = 1.5f;

    [SerializeField, Tooltip("Maximum bob position offset.")] private Vector3 bobLimit = new Vector3(0.015f, 0.035f, 0f);

    [SerializeField, Tooltip("Rotation intensity while bobbing.")] private Vector3 rotationMultiplier = new Vector3(2.5f, 0f, 2f);


    private Vector2 walkInput;

    private Vector3 initialPos;
    private Quaternion initialRot;

    private Vector3 bobPosition;
    private Vector3 bobEulerRotation;

    private float bobTime;


    private void Awake()
    {
        initialPos = transform.localPosition;
        initialRot = transform.localRotation;
    }


    private void LateUpdate()
    {
        if (!SettingsManager.Instance.headBobbing) return;
        UpdateBobOffset();
        UpdateBobRotation();
        ApplyEffect();
    }


    public void GetWalkInput(InputAction.CallbackContext ctx)
    {
        walkInput = ctx.ReadValue<Vector2>();
    }


    private void UpdateBobOffset()
    {
        float bobMult = 1f;

        if (controller.IsSprinting)
            bobMult = sprintMultiplier;
        else if (controller.IsCrouching)
            bobMult = crouchedMultiplier;

        float moveAmount = walkInput.magnitude;

        if (moveAmount > 0.01f)
            bobTime += Time.deltaTime * bobFrequency * bobMult;


        float bobSin = Mathf.Sin(bobTime);
        float bobCos = Mathf.Cos(bobTime);

        float idleBob = Mathf.Sin(Time.time * idleFrequency) * idleAmplitude;


        bobPosition.x = bobCos * bobLimit.x * moveAmount * bobAmplitude;

        bobPosition.y =
            Mathf.Abs(bobSin) *
            bobLimit.y *
            moveAmount *
            bobAmplitude +
            idleBob;

        bobPosition.z = 0f;
    }


    private void UpdateBobRotation()
    {
        float moveAmount = walkInput.magnitude;

        float bobSin = Mathf.Sin(bobTime);


        // Head dipping while walking
        bobEulerRotation.x = -Mathf.Abs(bobSin) * rotationMultiplier.x * moveAmount;


        // No unnecessary yaw
        bobEulerRotation.y = 0f;


        // Natural head roll
        bobEulerRotation.z = bobSin * rotationMultiplier.z * moveAmount;
    }


    private void ApplyEffect()
    {
        Vector3 targetPosition = initialPos + bobPosition;
        Quaternion targetRotation = initialRot * Quaternion.Euler(bobEulerRotation);


        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            Time.deltaTime * positionSmooth);


        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            Time.deltaTime * rotationSmooth);
    }
}