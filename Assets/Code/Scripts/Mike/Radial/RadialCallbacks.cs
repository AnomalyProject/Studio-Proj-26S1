using PurrNet;
using Steamworks;
using UnityEngine;

public class RadialCallbacks : MonoBehaviour
{
    public void SendTextMessage(string msg)
    {
        ulong ownerSteamID = SteamUser.GetSteamID().m_SteamID;
        TextChatManager.Instance.SendChatMessage(msg, ownerSteamID);
    }

    [ObserversRpc] public static void PlaySoundFromPlayer_Observers(AudioClip clip) => PlayerBody.localPlayerBody.AudioSource.PlayOneShot(clip);
}
