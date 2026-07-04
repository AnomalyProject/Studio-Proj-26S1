using System;
using UnityEngine;

public class EnemyHitCheck : MonoBehaviour
{
    private bool canHit;
    public bool CanHit => canHit;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            canHit = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            canHit = false;
        }
    }
}
