using UnityEngine;

public class SecurityCameraMovement : MonoBehaviour
{
    [SerializeField] private int angleLimit = 60;
    [SerializeField] private float movementSpeed = 1.5f;
    private Quaternion rotation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rotation = transform.localRotation;
    }

    // Update is called once per frame
    void Update()
    {
        float curve = Mathf.Sin(Time.time * movementSpeed);

        float currentAngle = curve * angleLimit;

        transform.localRotation = rotation * Quaternion.Euler(0, currentAngle, 0);
    }
}

