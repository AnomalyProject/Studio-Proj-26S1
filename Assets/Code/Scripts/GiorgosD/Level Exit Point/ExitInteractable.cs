using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ExitInteractable : MonoBehaviour, IInteractable<PlayerBody>
{
    [SerializeField] protected LevelExitPoint exitPoint;

     void Start()
    {
        GetComponent<Collider>().isTrigger = false; // Ensure the collider is not a trigger for interaction

        if (exitPoint == null)
        {
            exitPoint = GetComponentInParent<LevelExitPoint>();
            if (exitPoint == null) Debug.LogError($"{gameObject.name}: No LevelExitPoint found for interaction. Please assign one.");
        }
    }
    public Task<bool> CanInteract(PlayerBody interactor) => exitPoint.CanInteract(interactor);
    public Task<bool> TryInteract(PlayerBody interactor) => exitPoint.TryInteract(interactor);
}
