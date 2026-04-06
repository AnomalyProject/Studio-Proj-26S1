using UnityEngine;

public class ShowHideAll : MonoBehaviour
{
    public void ShowOnly()
    {
        CanvasManager.Instance.ShowOnly(CanvasManager.CanvasID.MainMenu);
    }

    public void HideAll()
    {
        CanvasManager.Instance.HideAll();
    }
}
