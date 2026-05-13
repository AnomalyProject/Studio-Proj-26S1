using UnityEngine;

public class InputLock : MonoBehaviour
{
    [SerializeField] private InputBridge.InputContext inputContext;
    private void Awake()
    {
        InputBridge.ClearContextStack();
        InputBridge.LockAt(inputContext);
    }
    private void OnDestroy() => InputBridge.Unlock();
}