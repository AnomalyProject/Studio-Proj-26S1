using UnityEngine;

public class TextChatUIRegistry : MonoBehaviour {
    [Header("Reference helper for TextChatManager")]
    [SerializeField] private GameObject localChatCanvas;

    private void Start() {
        if (TextChatManager.Instance != null) {
            TextChatManager.Instance.chatCanvas = localChatCanvas;

            Debug.Log("UI References successfully passed to TextChatManager");
        }
        else {
            Debug.LogError("TextChatManager Instance not found");
        }
    }
}