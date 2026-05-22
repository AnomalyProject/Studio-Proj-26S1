using UnityEngine;

public class LightSwitcher : MonoBehaviour
{
    [SerializeField] private Light[] lights;
    [SerializeField] private int index = 0;

    private void Awake() => lights[index].enabled = true;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Home))
        {
            lights[index++].enabled = false;
            if (index >= lights.Length) index = 0;
            lights[index].enabled = true;
            Debug.Log($"Set Directional Light to {lights[index].name}");
        }
    }
}
