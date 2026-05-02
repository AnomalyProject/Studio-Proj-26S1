using UnityEngine;

public abstract class SoundCaller : MonoBehaviour
{
    protected void PlayUIClip(AudioClip clip)
    {
        if (AudioManager.Instance && clip) AudioManager.Instance.PlayUI(clip);
    }

    protected void PlaySFXClip(AudioClip clip)
    {
        if (AudioManager.Instance && clip) AudioManager.Instance.PlaySFX(clip);
    }

    protected void PlayMusic(AudioClip clip)
    {
        if (AudioManager.Instance && clip) AudioManager.Instance.PlayMusic(clip);
    }

    protected void CrossfadeMusic(AudioClip clip)
    {
        if (AudioManager.Instance && clip) AudioManager.Instance.CrossFadeMusic(clip);
    }

    protected void FadeOutMusic(AudioClip clip)
    {
        if (AudioManager.Instance && clip) AudioManager.Instance.FadeOutMusic(clip);
    }
}
