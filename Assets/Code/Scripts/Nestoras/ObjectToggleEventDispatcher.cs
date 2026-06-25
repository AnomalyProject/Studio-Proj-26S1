using UnityEngine.Events;
using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Simple utility for triggering stuff when GameObjects are toggled
/// </summary>
public class ObjectToggleEventDispatcher : MonoBehaviour
{
    public UnityEvent onEnable = new UnityEvent();
    public UnityEvent onDisable = new UnityEvent();
    private void OnEnable() => onEnable.Invoke();
    private void OnDisable() => onDisable.Invoke();
}
