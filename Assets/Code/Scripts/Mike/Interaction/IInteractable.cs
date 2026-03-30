using UnityEngine;

public interface IInteractable<in TInteractor> where TInteractor : MonoBehaviour
{
    public bool CanInteract(TInteractor interactor);
    public bool TryInteract(TInteractor interactor);
}