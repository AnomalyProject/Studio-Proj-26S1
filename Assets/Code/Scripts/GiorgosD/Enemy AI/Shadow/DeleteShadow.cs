using UnityEngine;

public class DeleteShadow : MonoBehaviour
{
    [SerializeField] private ParticleSystem shadowParticles;
    
    void Update()
    {
        if (!shadowParticles.IsAlive())
        {
            Destroy(gameObject);
        }
    }
}
