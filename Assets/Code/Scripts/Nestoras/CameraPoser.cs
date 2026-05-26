using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(CameraPoser))]
public class LightSwitcherInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Next")) ((CameraPoser)target).Next();
    }
}
#endif

[ExecuteAlways]
public class CameraPoser : MonoBehaviour
{
    [SerializeField] private Transform cameraStack;
    [SerializeField] private Light[] lights;
    [SerializeField] private Transform[] transforms;
    [SerializeField] private int index = 0;

    private void Awake()
    {
        foreach (Light light in lights) light.gameObject.SetActive(false);
        lights[index].gameObject.SetActive(true);
        cameraStack.position = transforms[index].position;
        cameraStack.rotation = transforms[index].rotation;
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
        cameraStack.position = transforms[index].position;
        cameraStack.rotation = transforms[index].rotation;
        Debug.Log($"Set View to {lights[index].name.Replace(" Dir Light", string.Empty)}");
    }
}
