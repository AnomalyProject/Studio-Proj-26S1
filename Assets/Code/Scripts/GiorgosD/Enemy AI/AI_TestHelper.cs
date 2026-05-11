using UnityEngine;
using UnityEngine.Events;

public class AI_TestHelper : MonoBehaviour
{
    [SerializeField] private EnemyPawn body;
    [SerializeField] private EnemyBrain brain;

    [SerializeField] private Transform player;
    [SerializeField] private AudioSource audioSource;
    public UnityEvent<Transform> onWatched;
    public UnityEvent onItemPicked;


    void Update()
    {
        if (brain.tempStuffEnabled)
        {
            // Increase aggression level.
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                body.IncreaseAggression();
            }

            // Decrease aggression level.
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                body.DecreaseAggression();
            }

            // Shift patrol point priority. (Toggle)
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                onItemPicked.Invoke();
            }

            // Player is watching for too long.
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                onWatched.Invoke(player);
            }

            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                audioSource.Play();
            }
        }
    }
}
