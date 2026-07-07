using UnityEngine;

/// <summary>
/// Keeps the multiplayer session UI alive across lobby/gameplay scenes transitions.
/// </summary>
public class SessionUIRoot : MonoBehaviour
{
    public static SessionUIRoot Instance { get; private set; }
    public LobbyUI LobbyUI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}