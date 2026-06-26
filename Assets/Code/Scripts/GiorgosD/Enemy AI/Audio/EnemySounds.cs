using UnityEngine;

public class EnemySounds : MonoBehaviour
{
    [Header("Refrence")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private int minFootSteps = 0;
    [SerializeField] private int maxFootSteps = 4;
    
    [Header("Audio Sounds")]
    [SerializeField] private AudioClip[] footSteps;

    public void FootStep()
    {
        int index = Random.Range(minFootSteps, maxFootSteps);
        
        audioSource.PlayOneShot(footSteps[index]);
    }
}
