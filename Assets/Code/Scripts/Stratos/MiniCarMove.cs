using UnityEngine;

public class MiniCarMove : MonoBehaviour
{
    [SerializeField] private float driveSpeed = 5f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float detectRange = 0.5f;
    [SerializeField] private float frontOffset = 0.3f;

    private bool isTurning = false;
    private float rotationY;

    // Update is called once per frame
    void Update()
    {
        if (isTurning)
        {
            Turn();
        }
        else
        {
            DriveForward();
        }
    }

    private void DriveForward()
    {
        transform.Translate(Vector3.forward * driveSpeed * Time.deltaTime, Space.Self);

        Vector3 raycast = transform.position + (transform.forward * frontOffset);
        Ray ray = new Ray(raycast, transform.forward);

        Debug.DrawRay(raycast, transform.forward * detectRange, Color.green);

        if (Physics.Raycast(ray, detectRange))
        {
            rotationY = transform.localEulerAngles.y + Random.Range(100f, 260f);
            isTurning = true;
        }
    }

    private void Turn()
    {
        float currentY = transform.localEulerAngles.y;
        float nextY = Mathf.MoveTowardsAngle(currentY, rotationY, turnSpeed * Time.deltaTime);

        transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, nextY, transform.localEulerAngles.z);

        if (Mathf.Abs(Mathf.DeltaAngle(nextY, rotationY)) < 2f)
        {
            isTurning = false;
        }
    }
}
