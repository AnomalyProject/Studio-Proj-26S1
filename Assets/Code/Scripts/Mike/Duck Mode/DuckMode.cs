using PurrNet;
using UnityEngine;

public class DuckMode : NetworkBehaviour
{
    public static DuckMode Instance;
    [HideInInspector] public SyncVar<bool> modeActive = new(false, ownerAuth: false);

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [ServerRpc] public void ToggleMode_ServerRpc() => modeActive.value = !modeActive.value;
}