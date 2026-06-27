using UnityEngine;

public class RimOrbit : MonoBehaviour
{
    [Header("Orbit Settings")]
    public Transform hoopCenter;
    public float orbitSpeed = 150f;

    [Header("Self-Spin Settings")]
    public Vector3 spinSpeed = new Vector3(100f, 50f, 0f);

    void Update()
    {
        if (hoopCenter != null)
        {
            // CHANGED: Uses hoopCenter.up instead of Vector3.up
            // Now, if you tilt the hoop, the orbit tilts with it!
            transform.RotateAround(hoopCenter.position, hoopCenter.up, orbitSpeed * Time.deltaTime);
        }
        else
        {
            Debug.LogWarning("Hoop Center is not assigned in the Inspector!");
        }

        transform.Rotate(spinSpeed * Time.deltaTime, Space.Self);
    }
}