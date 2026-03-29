using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ExitInteractable : MonoBehaviour, IInteractable<FPSController>
{
    [SerializeField] LevelExitPoint exitPoint;

     void Start()
    {
        GetComponent<Collider>().isTrigger = false; // Ensure the collider is not a trigger for interaction

        if (exitPoint == null)
        {
            exitPoint = GetComponentInParent<LevelExitPoint>();
            if (exitPoint == null) Debug.LogError($"{gameObject.name}: No LevelExitPoint found for interaction. Please assign one.");
        }
    }
    public bool CanInteract(FPSController interactor) => exitPoint.CanInteract(interactor);
    public bool TryInteract(FPSController interactor) => exitPoint.TryInteract(interactor);
}
