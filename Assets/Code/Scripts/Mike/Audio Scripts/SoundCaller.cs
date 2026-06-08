using UnityEngine;

public class SoundCaller : MonoBehaviour
{
    public void PlayUIClip(AudioClip clip)
    {
        if (AudioManager.Instance && clip) AudioManager.Instance.PlayUI(clip);
    }

    public void PlaySFXClip(AudioClip clip)
    {
        if (AudioManager.Instance && clip) AudioManager.Instance.PlaySFX(clip);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (AudioManager.Instance && clip) AudioManager.Instance.PlayMusic(clip);
    }
    public void StopMusic()
    {
        if (AudioManager.Instance) AudioManager.Instance.StopMusic();
    }

    public void CrossfadeMusic(AudioClip clip)
    {
        if (AudioManager.Instance && clip) AudioManager.Instance.CrossFadeMusic(clip);
    }

    public void FadeOutMusic(AudioClip clip)
    {
        if (AudioManager.Instance) AudioManager.Instance.FadeOutMusic(clip);
    }
    public void FadeOutMusic()
    {
        if (AudioManager.Instance) AudioManager.Instance.FadeOutMusic(null);
    }
}
