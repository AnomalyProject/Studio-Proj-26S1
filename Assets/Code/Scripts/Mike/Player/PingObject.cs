using PurrNet;
using UnityEngine;
using UnityEngine.UI;

public class PingObject : NetworkBehaviour
{
    [SerializeField] private float scaleMultiplier = 0.5f;
    [SerializeField] private Image pingImage;

    private void LateUpdate()
    {
        if (PlayerBody.localPlayerBody == null) return;
        Transform target = PlayerBody.localPlayerBody.CameraController.transform;
        transform.LookAt(target.position, target.up);

        float distance = Vector3.Distance(target.position, transform.position);
        float scale = distance * scaleMultiplier;

        transform.localScale = Vector3.one * scale;
    }

    [ObserversRpc(bufferLast: true)]
    public void SetColor_Observers(Color color, int index)
    {
        if (pingImage != null)
        {
            pingImage.color = PlayerColour.GetColor(index);
            pingImage.color = color;
        }
    }
}