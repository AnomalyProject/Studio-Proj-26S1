using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KickVotePanelUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text reasonText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private void Awake()
    {
        yesButton.onClick.AddListener(() => SessionManager.Instance.RequestCastKickVote(true));
        noButton.onClick.AddListener(() => SessionManager.Instance.RequestCastKickVote(false));
        root.SetActive(false);
    }

    private void OnEnable()
    {
        SessionEvents.OnKickVoteUpdated += HandleVoteUpdated;
        SessionEvents.OnKickVoteFinished += HandleVoteFinished;
    }

    private void OnDisable()
    {
        SessionEvents.OnKickVoteUpdated -= HandleVoteUpdated;
        SessionEvents.OnKickVoteFinished -= HandleVoteFinished;
    }

    private void HandleVoteUpdated(ClientKickVoteData data)
    {
        root.SetActive(data.HasActiveVote);

        if (!data.HasActiveVote) return;

        titleText.text = $"Remove {data.TargetDisplayName}?";
        reasonText.text = data.Reason.ToDisplayText();
        progressText.text = $"{data.YesVotes}/{data.RequiredYesVotes} yes votes";
        timerText.text = $"{Mathf.CeilToInt(data.RemainingSeconds)}s";
    }

    private void HandleVoteFinished(bool succeeded, string message)
    {
        root.SetActive(false);
    }
}