using UnityEngine;

public class MenuMock : MonoBehaviour
{
    public void OpenSettings()
    {
        CanvasManager.Instance.Show(CanvasManager.CanvasID.Settings);
    }

    public void OpenPauseMenu()
    {
        CanvasManager.Instance.Show(CanvasManager.CanvasID.PauseMenu);
    }

    public void KillSwitch()
    {
        CanvasManager.Instance.Show(CanvasManager.CanvasID.HUD);
    }
}
