using UnityEngine;

public class SettingMock : MonoBehaviour
{
    public void CloseSettings()
    {
        CanvasManager.Instance.Hide(CanvasManager.CanvasID.Settings);
    }
}
