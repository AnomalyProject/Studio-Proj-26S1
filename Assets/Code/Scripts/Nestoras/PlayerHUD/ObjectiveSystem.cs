using UnityEngine;
using TMPro;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Simple system for updating the objective on the player's HUD based on the player's progress.
/// </summary>
public class ObjectiveSystem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI objective;

    private void Awake()
    {
        GameManager.OnInitialized += InitManager;
        GameManager.OnDestroyed += HandleManagerDestruction;
    }
    private void OnDestroy()
    {
        GameManager.OnInitialized -= InitManager;
        GameManager.OnDestroyed -= HandleManagerDestruction;
    }
    private void InitManager(GameManager gameManager)
    {
        if (gameManager == null) return;

        gameManager.OnGameReset.AddListener(ResetTutorial);
        gameManager.OnProgressChanged.AddListener(UpdateObjective);
        gameManager.OnWrongDecision.AddListener(ExplainVoid);

        gameManager.AnomalyManager.OnStateChanged += HandleStateChanged;

        ResetTutorial();
    }

    private void HandleStateChanged(AnomalyManager.RoomState state)
    {
        if (state != AnomalyManager.RoomState.PunishmentRoom) UpdateObjective(GameManager.Instance.CurrentProgress);
    }

    private void HandleManagerDestruction(GameManager gameManager)
    {
        gameManager.AnomalyManager.OnStateChanged -= HandleStateChanged;
        objective.enabled = false;
    }

    private void ExplainVoid()
    {
        objective.enabled = true;
        objective.text = "Get to the exit before the timer runs out to keep your progress";
    }

    private void UpdateObjective(int progress)
    {
        objective.enabled = true;
        if (progress == 1) objective.text = "If you spot anything different, turn back. Otherwise, keep going.";
        else if (progress != 0) objective.enabled = false;
    }

    private void ResetTutorial()
    {
        objective.enabled = true;
        objective.text = "Memorize the layout, and move to the next elevator.";
    }
}
