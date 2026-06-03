using PurrNet;
using UnityEngine;

public class AnimToEnemyBridge : NetworkBehaviour
{
    [SerializeField] private EnemyPawn body;
    
    public void AttackPlayer()
    {
        if (!isServer) return;
        
        body.StartAttack();
    }
}
