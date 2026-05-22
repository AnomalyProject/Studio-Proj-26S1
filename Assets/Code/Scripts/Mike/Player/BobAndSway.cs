using UnityEngine;
using UnityEngine.InputSystem;

public class BobAndSway : MonoBehaviour
{
    [SerializeField] private FPSController controller;

    [Header("UpdateSway Position")]
    [SerializeField, Tooltip("Position offset intensity from mouse movement.")] private float step = 0.01f;
    [SerializeField, Tooltip("Maximum sway position distance.")] private float maxStepDistance = 0.06f;


    [Header("UpdateSway Rotation")]
    [SerializeField, Tooltip("Rotation intensity from mouse movement.")] private float rotationStep = 4f;
    [SerializeField, Tooltip("Maximum sway rotation.")] private float maxRotationStep = 5f;


    [Header("Smoothing")]
    [SerializeField, Tooltip("Position smoothing speed.")] private float positionSmooth = 10f;
    [SerializeField, Tooltip("Rotation smoothing speed.")] private float rotationSmooth = 12f;


    [Header("Idle Breathing")]
    [SerializeField, Tooltip("Breathing speed.")] private float idleFrequency = 1.5f;
    [SerializeField, Tooltip("Breathing amplitude.")] private float idleAmplitude = 0.0025f;


    [Header("Bobbing")]
    [SerializeField, Tooltip("Movement bobbing speed.")] private float bobFrequency = 8f;
    [SerializeField, Min(1), Tooltip("Bobbing Multiplier applied while sprinting.")] private float sprintMultiplier = 1.5f;
    [SerializeField, Range(.1f, 1), Tooltip("Bobbing Multiplier applied while crouching.")] private float crouchedMultiplier = 0.5f;
    [SerializeField, Min(0), Tooltip("Overall bob intensity.")] private float bobAmplitude = 1f;
    [SerializeField, Tooltip("Directional movement offsets.")] private Vector3 travelLimit = Vector3.one * 0.025f;
    [SerializeField, Tooltip("Maximum bob position offset.")] private Vector3 bobLimit = Vector3.one * 0.01f;
    [SerializeField, Tooltip("Rotation intensity while bobbing.")] private Vector3 rotationMultiplier = Vector3.one;

    private Vector2 walkInput;
    private Vector2 lookInput;
    private Vector3 initialPos;
    private Vector3 swayPos;
    private Vector3 swayEulerRot;
    private Vector3 bobPosition;
    private Vector3 bobEulerRotation;
    private float bobTime;

    private void Awake() => initialPos = transform.localPosition;

    private void LateUpdate()
    {
        UpdateSway();
        UpdateSwayRotation();
        UpdateBobOffset();
        UpdateBobRotation();
        ApplyEffect();
    }

    public void GetWalkInput(InputAction.CallbackContext ctx) => walkInput = ctx.ReadValue<Vector2>();
    public void GetLookInput(InputAction.CallbackContext ctx) => lookInput = ctx.ReadValue<Vector2>();

    private void UpdateSway()
    {
        Vector3 invertLook = lookInput * -step;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxStepDistance, maxStepDistance);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxStepDistance, maxStepDistance);
        swayPos = invertLook;
    }

    private void UpdateSwayRotation()
    {
        Vector2 invertLook = lookInput * -rotationStep;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxRotationStep, maxRotationStep);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxRotationStep, maxRotationStep);
        swayEulerRot = new Vector3(invertLook.y, invertLook.x, invertLook.x);
    }

    private void UpdateBobOffset()
    {
        float bobMult = 1;
        if (controller.IsSprinting) bobMult = sprintMultiplier;
        else if (controller.IsCrouching) bobMult = crouchedMultiplier;

        float moveAmount = walkInput.magnitude;
        if (controller.IsGrounded && moveAmount > 0.01f) bobTime += Time.deltaTime * bobFrequency * bobMult;

        float bobSin = Mathf.Sin(bobTime);
        float bobCos = Mathf.Cos(bobTime);
        float idleBob = Mathf.Sin(Time.time * idleFrequency) * idleAmplitude;

        bobPosition.x = (bobCos * bobLimit.x * moveAmount * bobAmplitude) + idleBob - (walkInput.x * travelLimit.x);
        bobPosition.y = (bobSin * bobLimit.y * moveAmount * bobAmplitude) + idleBob - Mathf.Abs(walkInput.y * travelLimit.y);
        bobPosition.z = -(walkInput.y * travelLimit.z);
    }

    private void UpdateBobRotation()
    {
        float moveAmount = walkInput.magnitude;
        float bobSin = Mathf.Sin(bobTime);
        float bobCos = Mathf.Cos(bobTime);

        bobEulerRotation.x = bobSin * rotationMultiplier.x * moveAmount;
        bobEulerRotation.y = bobCos * rotationMultiplier.y * moveAmount;
        bobEulerRotation.z = bobCos * rotationMultiplier.z * walkInput.x;
    }

    private void ApplyEffect()
    {
        Vector3 targetPosition =  initialPos + swayPos + bobPosition;
        Quaternion targetRotation = Quaternion.Euler(swayEulerRot + bobEulerRotation);

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * positionSmooth);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * rotationSmooth);
    }
}