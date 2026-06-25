using PurrNet;
using UnityEngine;

public class HoverEffect : MonoBehaviour
{
    [SerializeField] private float hoverSpeed = 2f;
    [SerializeField] private float hoverUpDown = 0.2f;

    [SerializeField] private bool rotation = true;
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 10f, 0f);

    private Vector3 startPos;
    private void OnEnable()
    {
        startPos = transform.position;
    }
    private void Update()
    {
        ApplyHoverEffect();
    }


    private void ApplyHoverEffect()
    {
        float hoverY = startPos.y + (Mathf.Sin(Time.time * hoverSpeed) * hoverUpDown);

        transform.position = new Vector3(startPos.x, hoverY, startPos.z);

        if (rotation)
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }
    }
}
