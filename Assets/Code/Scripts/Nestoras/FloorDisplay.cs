using UnityEngine;
using TMPro;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Temporary script to display the game's progress.
/// </summary>
public class FloorDisplay : MonoBehaviour
{
    private GameManager gameManager;
    private TextMeshPro floorNumber;

    private void Awake()
    {
        floorNumber = GetComponent<TextMeshPro>();
        Debug.LogWarning(floorNumber);
        gameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        if (gameManager != null) gameManager.OnProgressChanged.AddListener((progress) => floorNumber.text = $"{progress}/{gameManager.CorrectDecisionsToWin()}");
    }
}
