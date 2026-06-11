using Unity.VisualScripting;
using UnityEngine;

public class Test_Persistant : MonoBehaviour
{
    public static Test_Persistant Instance;

    //bro
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
