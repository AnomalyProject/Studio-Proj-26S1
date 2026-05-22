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
        Vector3 targetPos = PlayerBody.localPlayerBody.CameraController.transform.position;
        transform.LookAt(targetPos);

        float distance = Vector3.Distance(targetPos, transform.position);

        float scale = distance * scaleMultiplier;

        transform.localScale = Vector3.one * scale;
    }

    [ObserversRpc] public void SetColor_Observers(Color color) => pingImage.color = color;
}