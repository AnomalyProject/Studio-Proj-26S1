using System;
using System.Linq;
using UnityEngine;

public class InteractionSystem<TInteractor> where TInteractor : MonoBehaviour
{
    /// <summary>
    /// Occurs when an interaction attempt is made with the interactable object.
    /// </summary>
    /// <remarks>
    /// Provides the interactable and a boolean value indicating whether the attempt was successful.
    /// Null checks for the interactable are advised, in case it gets destroyed.
    /// </remarks>
    public event Action<IInteractable<TInteractor>, bool> OnInteractionAttempted;
    public event Action<IInteractable<TInteractor>> OnFocusedInteractable, OnInteractableLostFocus;


    #region Fields & Properties
    IInteractable<TInteractor> _focusedInteractable;
    readonly TInteractor Interactor;

    /// <summary>
    /// Gets the interactable object that currently has focus for this interactor. Can be null.
    /// </summary>
    public IInteractable<TInteractor> FocusedInteractable => _focusedInteractable;
    #endregion

    public InteractionSystem(TInteractor interactor)
    {
        this.Interactor = interactor;
    }

    /// <summary>
    /// Attempts to interact with the currently focused interactable object, if one is available and can be interacted
    /// with.
    /// </summary>
    /// <remarks>Use this method to perform an interaction with the object currently in focus. If there is no
    /// focused interactable or it cannot be interacted with, the method returns false.</remarks>
    /// <returns>true if the focused interactable object exists and the interaction succeeds; otherwise, false.</returns>
    public bool TryInteractFocused()
    {
        if (_focusedInteractable != null && _focusedInteractable.CanInteract(Interactor))
        {
            bool success = _focusedInteractable.TryInteract(Interactor);
            OnInteractionAttempted?.Invoke(_focusedInteractable, success);
            return success;
        }

        return false;
    }

    #region Scanning

    #region Raycasting

    /// <summary>
    /// Performs a raycast in the scene to detect and focus on the first interactable object that can be interacted with
    /// by the current interactor.
    /// </summary>
    /// <remarks>If an interactable object is detected and can be interacted with, it becomes the currently
    /// focused interactable. Otherwise, the focus is cleared. This method is typically used to update the current
    /// target for interaction based on the user's aim or viewpoint.</remarks>
    public void RaycastScan(Ray ray, float maxDistance, LayerMask layerMask)
    {
        if(maxDistance < 0) return;

        if (Physics.Raycast(ray, out RaycastHit hitInfo, maxDistance, layerMask))
        {
            if(hitInfo.collider.TryGetComponent(out IInteractable<TInteractor> interactable) 
                && interactable.CanInteract(Interactor))
            {
                ChangeFocused(interactable);
                return;
            }
        }

        ChangeFocused(null);
    }

    /// <inheritdoc cref="RaycastScan(Ray, float, LayerMask)"/>
    public void RaycastScan(Camera camera, float maxDistance, LayerMask layerMask)
    {
        if (!camera) return;

        Vector3 screenCenter = camera.pixelRect.center;
        Ray ray = camera.ScreenPointToRay(screenCenter);
        RaycastScan(ray, maxDistance, layerMask);
    }

    /// <inheritdoc cref="RaycastScan(Ray, float, LayerMask)"/>
    public void RaycastScan(Ray ray, float maxDistance) => RaycastScan(ray, maxDistance, ~0);

    /// <inheritdoc cref="RaycastScan(Ray, float, LayerMask)"/>
    public void RaycastScan(Camera camera, float maxDistance) => RaycastScan(camera, maxDistance, ~0);

    #endregion

    #region OverlapSphere

    /// <summary>
    /// Scans for interactable objects within a sphere at the specified position and radius, updating the currently
    /// focused interactable if one is found.
    /// </summary>
    /// <remarks>If multiple interactable objects are found, the closest valid one along an unobstructed line
    /// of sight is selected as the focused interactable. If no valid interactable is found, the focus is
    /// cleared.</remarks>
    public void OverlapSphereScan(Vector3 position, float radius, LayerMask layerMask)
    {
        if(radius < 0) return;

        IOrderedEnumerable<Collider> colliders = Physics.OverlapSphere(position, radius, layerMask)
            .OrderBy(collider => Vector3.Distance(position, collider.transform.position));

        foreach (Collider collider in colliders)
        {
            if(collider.TryGetComponent(out IInteractable<TInteractor> interactable) && interactable.CanInteract(Interactor))
            {
                Ray validationRay = new Ray(position, collider.transform.position - position);
                float distance = Vector3.Distance(position, collider.transform.position);
                bool rayHit = Physics.Raycast(validationRay, out RaycastHit hitInfo, distance);

                if (rayHit && hitInfo.collider == collider)
                {
                    ChangeFocused(interactable);
                    return;
                }
            }
        }

        ChangeFocused(null);
    }

    /// <inheritdoc cref="OverlapSphereScan(Vector3, float, LayerMask)"/>
    public void OverlapSphereScan(Transform transform, float radius, LayerMask layerMask) => OverlapSphereScan(transform.position, radius, layerMask);

    /// <inheritdoc cref="OverlapSphereScan(Vector3, float, LayerMask)"/>
    public void OverlapSphereScan(Vector3 position, float radius) => OverlapSphereScan(position, radius, ~0);

    /// <inheritdoc cref="OverlapSphereScan(Vector3, float, LayerMask)"/>
    public void OverlapSphereScan(Transform transform, float radius) => OverlapSphereScan(transform.position, radius, ~0);

    #endregion

    #endregion

    #region Helpers

    void ChangeFocused(IInteractable<TInteractor> newFocus)
    {
        if (_focusedInteractable == newFocus) return;

        if (_focusedInteractable != null)
            OnInteractableLostFocus?.Invoke(_focusedInteractable);

        _focusedInteractable = newFocus;

        if (_focusedInteractable != null)
            OnFocusedInteractable?.Invoke(_focusedInteractable);
    }

    #endregion

}
