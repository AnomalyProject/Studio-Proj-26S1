using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(LightSwitcher))]
public class LightSwitcherInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Next")) ((LightSwitcher)target).Next();
    }
}
#endif

[ExecuteAlways]
public class LightSwitcher : MonoBehaviour
{
    [SerializeField] private Light[] lights;
    [SerializeField] private int index = 0;

    private void Awake()
    {
        foreach (Light light in lights) light.gameObject.SetActive(false);
        lights[index].gameObject.SetActive(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Home)) Next();
    }

    public void Next()
    {
        lights[index++].gameObject.SetActive(false);
        if (index >= lights.Length) index = 0;
        lights[index].gameObject.SetActive(true);
        Debug.Log($"Set Directional Light to {lights[index].name}");
    }
}
