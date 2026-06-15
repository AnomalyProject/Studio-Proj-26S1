using UnityEngine;
using TMPro;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Simple system for updating the objective on the player's HUD during the tutorial.
/// </summary>
public class ObjectiveSystem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI objective;

    private void Awake()
    {
        TutorialManager.OnInitialized += InitManager;
        TutorialManager.OnDestroyed += HandleManagerDestruction;
    }
    private void OnDestroy()
    {
        TutorialManager.OnInitialized -= InitManager;
        TutorialManager.OnDestroyed -= HandleManagerDestruction;
    }
    private void InitManager(TutorialManager tutorialManager)
    {
        if (tutorialManager == null) return;

        objective.enabled = true;
        tutorialManager.OnObjectiveUpdated.AddListener(UpdateObjective);
    }
    private void HandleManagerDestruction(TutorialManager tutorialManager) => objective.enabled = false;
    private void UpdateObjective(string newObjective) => objective.text = newObjective;
}
