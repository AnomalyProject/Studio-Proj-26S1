using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Script to make sure only one player canvas is loaded.
/// </summary>
public class PlayerCanvas : MonoBehaviour
{
    public static PlayerCanvas Instance { get; private set; }

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
}
