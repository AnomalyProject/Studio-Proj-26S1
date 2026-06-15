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
        gameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        if (gameManager != null) gameManager.OnProgressChanged.AddListener((progress) => floorNumber.text = $"{progress}/{gameManager.CorrectDecisionsToWin()}");
        else // If no GameManager present, maybe we're in the Tutorial?
        {
            TutorialManager tutorialManager = FindFirstObjectByType<TutorialManager>(FindObjectsInactive.Include);
            if (tutorialManager != null) tutorialManager.onFloorChanged.AddListener(floor => floorNumber.text = floor);
        }
    }
}
