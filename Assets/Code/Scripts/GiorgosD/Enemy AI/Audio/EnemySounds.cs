using PurrNet;
using UnityEngine;

public class EnemySounds : NetworkBehaviour
{
    #region Variables 
    [Header("Refrence")] 
    [SerializeField] private EnemyBrain brain;
    
    [SerializeField] private AudioSource feetAudio;
    private int minFootSteps = 0;
    private int maxFootSteps;
    
    [SerializeField] private AudioSource voiceAudio;

    private int minNormalGrowls = 0;
    private int maxNormalGrowls;
    
    private int minAlertedGrowls = 0;
    private int maxAlertedGrowls;
    
    private int minChaseGrowls = 0;
    private int maxChaseGrowls;
    
    private int minDamagedGrowls = 0;
    private int maxDamagedGrowls;

    private int minAttackGrowls = 0;
    private int maxAttackGrowls;

    private bool isMustPlay;
    
    [Header("Audio Sounds")]
    [SerializeField] private AudioClip[] footSteps;
    [SerializeField] private AudioClip[] normalGrowls;
    [SerializeField] private AudioClip[] alertedGrowls;
    [SerializeField] private  AudioClip[] chaseGrowls;
    [SerializeField] private AudioClip[] damagedGrowls;
    [SerializeField] private AudioClip[] attackGrowls;
    #endregion

    #region Unity lifecycle
    private void Awake()
    {
        maxFootSteps = footSteps.Length;
        maxNormalGrowls = normalGrowls.Length;
        maxAlertedGrowls = alertedGrowls.Length;
        maxChaseGrowls = chaseGrowls.Length;
        maxDamagedGrowls = damagedGrowls.Length;
        maxAttackGrowls = attackGrowls.Length;
    }
    #endregion

    #region FootSteps
    /// <summary>
    /// Gets called by anim event to play footstep sounds.
    /// </summary>
    [ObserversRpc]
    public void FootStep()
    {
        int index = Random.Range(minFootSteps, maxFootSteps);
        
        feetAudio.PlayOneShot(footSteps[index]);
    }
    #endregion

    #region Growls
    /// <summary>
    /// Helper that choses what kind of growl should play
    /// </summary>
    [ObserversRpc]
    public void SelectGrowl()
    {
        isMustPlay = brain.CurrentStateID == EnemyBrain.StateID.Alert ||
                     brain.CurrentStateID == EnemyBrain.StateID.Attack ||
                     brain.CurrentStateID == EnemyBrain.StateID.Stunned;
        
        switch (brain.CurrentStateID)
        {
            case EnemyBrain.StateID.Idle:
            case EnemyBrain.StateID.Patrol:
                Growl(minNormalGrowls, maxNormalGrowls, normalGrowls);
                break;
            case EnemyBrain.StateID.Alert:
                Growl(minAlertedGrowls, maxAlertedGrowls, alertedGrowls);
                break;
            case EnemyBrain.StateID.Chase:
                Growl(minChaseGrowls, maxChaseGrowls, chaseGrowls);
                break;
            case EnemyBrain.StateID.Stunned:
                Growl(minDamagedGrowls, maxDamagedGrowls, damagedGrowls);
                break;
            case EnemyBrain.StateID.Attack:
                Growl(minAttackGrowls, maxAttackGrowls, attackGrowls);
                break;
        }
    }
    
    /// <summary>
    /// Takes the Growl paramiters from Select growl to play the correct growl depending on the state of the enemy.
    /// </summary>
    /// <param name="minGrowl"></param>
    /// <param name="maxGrowl"></param>
    /// <param name="growls"></param>
    private void Growl(int minGrowl, int maxGrowl, AudioClip[] growls)
    {
        if (!isMustPlay && voiceAudio.isPlaying) return;

        if (isMustPlay)
        {
            voiceAudio.Stop();
        }
        
        int index = Random.Range(minGrowl, maxGrowl);
        
        voiceAudio.PlayOneShot(growls[index]);
    }
    #endregion
}
