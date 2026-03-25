using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

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
    [SerializeField] private float crossFadeDuration;
    [SerializeField] private float fadeDuration;

    private Coroutine musicTrnasitionCorouine;

    public event Action musicStart;
    public event Action musicStop;
    public event Action crossFadeStart;


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


    // Music methods



    /// <summary>
    /// The methods that external scripts can call to play sounds.
    /// </summary>
    /// <param name="clip"> The Audio clip to be played</param>
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
}
