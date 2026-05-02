using UnityEngine;
using UnityEngine.Events;

public class AI_TestHelper : MonoBehaviour
{
    [SerializeField] private EnemyPawn body;

    [SerializeField] private Transform player;
    public UnityEvent<Transform> onWatched;
    public UnityEvent onItemPicked;


    void Update()
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
    }
}
