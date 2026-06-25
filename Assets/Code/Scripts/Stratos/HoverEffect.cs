using PurrNet;
using UnityEngine;

public class HoverEffect : MonoBehaviour
{
    [SerializeField] private float hoverSpeed = 2f;
    [SerializeField] private float hoverUpDown = 0.2f;

    [SerializeField] private bool rotation = true;
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 10f, 0f);

    private Vector3 startLocPos;

    private void OnEnable()
    {
        startLocPos = transform.localPosition;
    }
    private void Update()
    {
        ApplyHoverEffect();
    }


    private void ApplyHoverEffect()
    {
        float hoverY = startLocPos.y + (Mathf.Sin(Time.time * hoverSpeed) * hoverUpDown);

        transform.position = new Vector3(startLocPos.x, hoverY, startLocPos.z);

        if (rotation)
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }
    }
}
