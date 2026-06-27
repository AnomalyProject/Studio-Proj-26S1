using UnityEngine;

public class EnemySounds : MonoBehaviour
{
    [Header("Refrence")]
    [SerializeField] private AudioSource feetAudio;
    [SerializeField] private int minFootSteps = 0;
    [SerializeField] private int maxFootSteps = 4;
    
    [SerializeField] private AudioSource voiceAudio;
    
    [Header("Audio Sounds")]
    [SerializeField] private AudioClip[] footSteps;

    public void FootStep()
    {
        int index = Random.Range(minFootSteps, maxFootSteps);
        
        feetAudio.PlayOneShot(footSteps[index]);
    }
}
