using UnityEngine;

public class NoClip : MonoBehaviour
{
    #region Veriables
    private static GameObject freeCam;
    private static GameObject localPlayer;
    private static Camera camCache;
    private static bool isNoClipActive;
    #endregion

    #region Asign NoClip
    /// <summary>
    /// Creates The noClip cheat by being called in Mikes InGameCheats script
    /// </summary>
    public static void CreateNoClip()
    {
        DevConsole.CommandData noClipComm = new DevConsole.CommandData("Enables/Disables the ability to fly out of your body and pass through walls.", NoClipCheat);
        DevConsole.RegisterCommand("free_cam", noClipComm);
    }
    #endregion

    #region Toggle NoClip
    /// <summary>
    /// Activation Function
    /// </summary>
    /// <param name="args"></param>
    private static void NoClipCheat(string[] args)
    {
        if (!isNoClipActive)
        {
            EnableNoClip();
            isNoClipActive = true;
        }
        else
        {
            DisableNoClip();
            isNoClipActive = false;
        }
    }
    #endregion
    
    #region Enabled NoClip
    /// <summary>
    /// Enables noClip
    /// </summary>
    private static void EnableNoClip()
    {
        foreach (var player in GameObject.FindObjectsByType<FPSController>(FindObjectsSortMode.InstanceID))
        {
            if (player.IsLocalPlayer)
            {
                localPlayer = player.gameObject;
                break;
            }
        }
        
        InputBridge.SetContext(InputBridge.InputContext.noClip);
        
        foreach (var cam in localPlayer.GetComponentsInChildren<Camera>())
        {
            cam.enabled = false;
        }
        
        CreateFreeCamObj();
    }
    #endregion
    
    #region Disabled NoClip
    /// <summary>
    /// Disables noClip
    /// </summary>
    private static void DisableNoClip()
    {
        Destroy(freeCam);
        foreach (var cam in localPlayer.GetComponentsInChildren<Camera>())
        {
            cam.enabled = true;
        }
        
        InputBridge.SetContext(InputBridge.InputContext.Player);
    }
    #endregion

    #region Create NoClip Object
    /// <summary>
    /// Creates noClip OBJ
    /// </summary>
    private static void CreateFreeCamObj()
    {
        freeCam = new GameObject("FreeCam");
        
        freeCam.AddComponent<Camera>();
        freeCam.AddComponent<NoClipController>();
        freeCam.transform.position = GameObject.Find("Main Camera").transform.position;
        freeCam.transform.rotation = GameObject.Find("Main Camera").transform.rotation;
        
    }
    #endregion
}
