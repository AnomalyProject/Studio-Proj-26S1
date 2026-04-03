using UnityEngine;

public class PauseMock : MonoBehaviour
{
    public void CosePauseMenu()
    {
        CanvasManager.Instance.Hide(CanvasManager.CanvasID.PauseMenu);
    }
}
