using System.Threading.Tasks;
using UnityEngine;

public interface IInteractable<in TInteractor> where TInteractor : MonoBehaviour
{
    public Task<bool> CanInteract(TInteractor interactor);
    public Task<bool> TryInteract(TInteractor interactor);
}