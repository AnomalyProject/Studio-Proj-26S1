using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicA;
    [SerializeField] private AudioSource musicB;
    private AudioSource activeMusic;
    [SerializeField] private List<AudioSource> sfxPool = new List<AudioSource>();
    [SerializeField] private List<AudioSource> uiPool = new List<AudioSource>();

    [Header("Audio Mixer Settings")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterVolume;
    [SerializeField] private string musicVolume;
    [SerializeField] private string sfxVolume;
    [SerializeField] private string uiVolume;

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup uiGroup;

    [Header("Fade In/Out Settings")]
    [SerializeField, Range(0.1f, 10.0f), Tooltip("Controls how long the CrossFade will last")] private float crossFadeDuration;
    [SerializeField, Range(0.1f, 10.0f), Tooltip("Controls how long the FadeOut/In will last")] private float fadeDuration;
    [SerializeField, Range(0f, 10.0f), Tooltip("Controls the pause between FadeOut and FadeIn")] private float fadePauseDelay;

    private Coroutine musicTranasitionCorouine;

    public event Action musicStart;
    public event Action musicStop;
    public event Action crossFadeStart;
    public event Action fadeInOutStart;

    #region Manager Object Setup
    private void Awake()
    {
        if (Instance != this && Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreateMusicSources();
    }

    /// <summary>
    /// Creates the music sources and sets musicA as the active music.
    /// </summary>
    private void CreateMusicSources()
    {
        musicA = gameObject.AddComponent<AudioSource>();
        musicB = gameObject.AddComponent<AudioSource>();

        Source(musicA, musicGroup, true);
        Source(musicB, musicGroup, true);

        activeMusic = musicA;
    }
    #endregion

    #region Source Setup
    /// <summary>
    /// Source setup method for sfx and ui audio sources.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="group"></param>
    /// <param name="loop"></param>
    private void Source(AudioSource source, AudioMixerGroup group, bool loop)
    {
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = group;
        source.loop = loop;
    }
    #endregion

    #region Volume Control
    /// <summary>
    /// Controls the volume.
    /// </summary>
    /// <param name="param"></param>
    /// <param name="normalizedVolume"> expects the float value to be 0.0-1.0 </param>
    public void Volume(string param, float normalizedVolume)
    {
        float dB = Mathf.Log10(Mathf.Clamp(normalizedVolume, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat(param, dB);
    }

    /// <summary>
    ///  Mutes/UnMutes all sounds
    /// </summary>
    /// <param name="isMute"></param>
    public void SetMute(bool isMute)
    {
        float volume = isMute ? -80.0f : 0f;
        audioMixer.SetFloat(masterVolume, volume);
    }

    /// <summary>
    /// Stops all sounds
    /// </summary>
    public void StopAllSounds()
    {
        StopMusic();
        uiPool.ForEach(source => source.Stop());
        sfxPool.ForEach(source => source.Stop());
    }
    #endregion

    #region Music Methods
    /// <summary>
    /// The three difrent ways to start music:
    /// PlayMusic: Starts the given clip immediately.
    /// CrossFadeMusic: CrossFades from the current music to the given clip.
    /// FadeOutMusic: Fades out the current music, waits for a short delay (if any) and then fades in the given clip.
    /// </summary>
    /// <param name="clip"></param>
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) 
        { 
            return; 
        }

        activeMusic.clip = clip;
        activeMusic.volume = 1f;
        activeMusic.Play();
        musicStart?.Invoke();
    }

    // See summary above PlayMusic
    public void CrossFadeMusic(AudioClip clip)
    {
        if (clip == null) 
        { 
            return; 
        }

        if (musicTranasitionCorouine != null)
        {
            StopCoroutine(musicTranasitionCorouine);
        }

        musicTranasitionCorouine = StartCoroutine(CrossFadeTransition(clip));
    }

    // See summary above PlayMusic
    public void FadeOutMusic(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (musicTranasitionCorouine != null)
        {
            StopCoroutine(musicTranasitionCorouine);
        }

        musicTranasitionCorouine = StartCoroutine(FadeOutTransition(clip));
    }

    /// <summary>
    /// Stops Music and resets the sources volume(in case of an IEnumarator being cut in the middle and the volume being stuck).
    /// </summary>
    public void StopMusic()
    {
        if (musicTranasitionCorouine != null)
        {
            StopCoroutine(musicTranasitionCorouine);
        }

        musicA.Stop();
        musicA.volume = 1f;

        musicB.Stop();
        musicB.volume = 1f;

        musicStop?.Invoke();
    }

    /// <summary>
    /// Pause/UnPause music
    /// </summary>
    public void PauseMusic() => activeMusic.Pause();
    public void UnPauseMusic() => activeMusic.UnPause();
    #endregion

    #region UI and SFX Methods
    /// <summary>
    /// The methods that external scripts can call to play sounds.
    /// </summary>
    /// <param name="clip"> The Audio clip to be played </param>
    public void PlaySFX(AudioClip clip) => PlaySound(clip, sfxGroup, sfxPool);
    public void PlayUI(AudioClip clip) => PlaySound(clip, uiGroup, uiPool);

    /// <summary>
    /// Plays the given audio clip.
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="group"></param>
    /// <param name="pool"></param>
    private void PlaySound(AudioClip clip, AudioMixerGroup group, List<AudioSource> pool)
    {
        if (clip == null) 
        { 
            return; 
        }

        AudioSource source = pool.Find(sound => !sound.isPlaying);

        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
            Source(source, group, false);
            pool.Add(source);
        }

        source.clip = clip;
        source.Play();
    }
    #endregion

    #region Transition IEnumerators
    /// <summary>
    /// The two IEnumerators that handle the music transitions logic.
    /// </summary>
    /// <param name="clip"></param>
    /// <returns></returns>
    private IEnumerator CrossFadeTransition(AudioClip clip)
    {
        crossFadeStart?.Invoke();

        AudioSource fadingOut = activeMusic;
        AudioSource fadingIn = (activeMusic == musicA) ? musicB : musicA;

        fadingIn.clip = clip;
        fadingIn.volume = 0f;
        fadingIn.Play();

        float fadeOutStartVolume = fadingOut.volume;

        float timer = 0f;
        while (timer < crossFadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / crossFadeDuration;

            fadingOut.volume = Mathf.Lerp(fadeOutStartVolume, 0f, progress);
            fadingIn.volume = Mathf.Lerp(0f, 1f, progress);

            yield return null;
        }

        fadingOut.Stop();
        fadingOut.volume = 0f;    // Safty check
        fadingIn.volume = 1f;     // Safty check

        activeMusic = fadingIn;

        musicTranasitionCorouine = null;
    }

    // See summary above CrossFadeTransition
    private IEnumerator FadeOutTransition(AudioClip clip)
    {
        fadeInOutStart?.Invoke();

        AudioSource source = activeMusic;

        // Fade out
        float startingVolume = source.volume;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;

            source.volume = Mathf.Lerp(startingVolume, 0f, progress);

            yield return null;
        }

        source.Stop();
        source.volume = 0f;    // Safty check

        // Delay Fade in start
        yield return new WaitForSeconds(fadePauseDelay);

        // Fade in
        source.clip = clip;
        source.Play();

        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;

            source.volume = Mathf.Lerp(0f, 1f, progress);

            yield return null;
        }

        source.volume = 1f;   // Safty check

        musicTranasitionCorouine = null;
    }
    #endregion
}
