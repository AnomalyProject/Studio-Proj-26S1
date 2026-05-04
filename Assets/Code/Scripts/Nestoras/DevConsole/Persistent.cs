using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Script to convert a GameObject into a persistent object that will not be destroyed on scene load.
/// ONLY USE THIS IN THE BOOTSTRAPPER!!!
/// </summary>
public class Persistent : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Destroy(this);
    }
}
