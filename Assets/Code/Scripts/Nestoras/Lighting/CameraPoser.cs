using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(CameraPoser))]
public class LightSwitcherInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Previous")) ((CameraPoser)target).Previous();
        if (GUILayout.Button("Reset")) ((CameraPoser)target).ResetPose();
        if (GUILayout.Button("Next")) ((CameraPoser)target).Next();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Copy to Scene")) ((CameraPoser)target).CopyToSceneView();
        Color defaultColor = GUI.backgroundColor;
        GUI.backgroundColor = ((CameraPoser)target).copyToGame ? Color.lawnGreen : Color.softRed;
        if (GUILayout.Button("Copy to Game"))
        {
            ((CameraPoser)target).copyToGame = !((CameraPoser)target).copyToGame;
        }
        GUI.backgroundColor = defaultColor;
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Save Game Pose")) ((CameraPoser)target).SaveCurrentView();
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

    [HideInInspector] public bool copyToGame;

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

#if UNITY_EDITOR
        if (!copyToGame) return;
        if (SceneView.lastActiveSceneView == null) return;
        cameraStack.position = SceneView.lastActiveSceneView.camera.transform.position;
        cameraStack.rotation = SceneView.lastActiveSceneView.camera.transform.rotation;
#endif
    }

    public void Previous()
    {
        index--;
        if (index < 0) index = lights.Length - 1;
        Debug.Log($"Set View to {lights[index].name.Replace(" Dir Light", string.Empty)}");
        ResetPose();
    }

    public void Next()
    {
        index++;
        if (index >= lights.Length) index = 0;
        Debug.Log($"Set View to {lights[index].name.Replace(" Dir Light", string.Empty)}");
        ResetPose();
    }

    public void ResetPose()
    {
        foreach (Light light in lights) light.gameObject.SetActive(false);
        lights[index].gameObject.SetActive(true);
        cameraStack.position = transforms[index].position;
        cameraStack.rotation = transforms[index].rotation;
    }

#if UNITY_EDITOR
    public void CopyToSceneView()
    {
        if (SceneView.lastActiveSceneView == null) return;
        SceneView.lastActiveSceneView.LookAt(cameraStack.position + cameraStack.forward * 2f, cameraStack.rotation, 1);
        SceneView.lastActiveSceneView.Repaint();
    }
    public void SaveCurrentView()
    {
        transforms[index].position = cameraStack.position;
        transforms[index].rotation = cameraStack.rotation;
        EditorUtility.SetDirty(transforms[index]);
    }
#endif
}