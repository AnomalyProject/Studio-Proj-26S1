using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Script to make sure only one tools canvas is loaded.
/// </summary>
public class ToolsCanvas : MonoBehaviour
{
    public static ToolsCanvas Instance { get; private set; }

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
