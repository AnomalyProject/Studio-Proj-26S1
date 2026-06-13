using PurrNet;
using Steamworks;
using UnityEngine;

public class RadialCallbacks : MonoBehaviour
{
    public void SendTextMessage(string msg)
    {
        if (!SteamIdentity.TryGetLocalSteamID(out ulong ownerSteamID)) return;
        TextChatManager.Instance.SendChatMessage(msg, ownerSteamID);
    }

    [ObserversRpc] public static void PlaySoundFromPlayer_Observers(AudioClip clip) => PlayerBody.localPlayerBody.AudioSource.PlayOneShot(clip);
}
