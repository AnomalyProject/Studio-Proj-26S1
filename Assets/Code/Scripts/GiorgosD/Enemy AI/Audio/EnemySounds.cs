using System.Linq;
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
    
    private int minChaseGrowls = 0;
    private int maxChaseGrowls;
    
    private int minDamagedGrowls = 0;
    private int maxDamagedGrowls;

    private int minAttackGrowls = 0;
    private int maxAttackGrowls;
    
    [Header("Audio Sounds")]
    [SerializeField] private AudioClip[] footSteps;
    [SerializeField] private AudioClip[] normalGrowls;
    [SerializeField] private  AudioClip[] chaseGrowls;
    [SerializeField] private AudioClip[] damagedGrowls;
    [SerializeField] private AudioClip[] attackGrowls;
    #endregion

    #region Unity lifecycle
    private void Awake()
    {
        maxFootSteps = footSteps.Length;
        maxNormalGrowls = normalGrowls.Length;
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
    public void SelectGrowl(EnemyBrain.StateID stateID)
    {
        switch (stateID)
        {
            case EnemyBrain.StateID.Idle:
            case EnemyBrain.StateID.Patrol:
                Growl(minNormalGrowls, maxNormalGrowls, normalGrowls, stateID);
                break;
            case EnemyBrain.StateID.Alert:
            case EnemyBrain.StateID.Chase:
                Growl(minChaseGrowls, maxChaseGrowls, chaseGrowls, stateID);
                break;
            case EnemyBrain.StateID.Stunned:
                Growl(minDamagedGrowls, maxDamagedGrowls, damagedGrowls, stateID);
                break;
            case EnemyBrain.StateID.Attack:
                Growl(minAttackGrowls, maxAttackGrowls, attackGrowls, stateID);
                break;
        }
    }
    
    /// <summary>
    /// Takes the Growl paramiters from Select growl to play the correct growl depending on the state of the enemy.
    /// </summary>
    /// <param name="minGrowl"></param>
    /// <param name="maxGrowl"></param>
    /// <param name="growls"></param>
    private void Growl(int minGrowl, int maxGrowl, AudioClip[] growls, EnemyBrain.StateID stateID)
    {
        if (voiceAudio.isPlaying)
        {
            if (stateID == EnemyBrain.StateID.Alert && voiceAudio.clip != null && chaseGrowls.Contains(voiceAudio.clip)) return;
            
            bool isMustPlay = stateID == EnemyBrain.StateID.Alert ||
                         stateID == EnemyBrain.StateID.Attack ||
                         stateID == EnemyBrain.StateID.Stunned;
            
            if (!isMustPlay) return;
            
            if (stateID == EnemyBrain.StateID.Chase && voiceAudio.clip != null && chaseGrowls.Contains(voiceAudio.clip)) return;
            
            voiceAudio.Stop();
        }
        
        int index = Random.Range(minGrowl, maxGrowl);
        
        voiceAudio.clip = growls[index];
        voiceAudio.Play();
    }
    #endregion
}
