using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class VersionDisplay : MonoBehaviour { 
    void Start() {
        TextMeshProUGUI versionText = GetComponent<TextMeshProUGUI>();
        versionText.text = "Build: v" + Application.version; 
    }
}