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
    
    private ClientKickVoteData currentVoteData;
    private float localVoteEndsAt;

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
    
    private void Update()
    {
        if (root == null || !root.activeSelf) return;

        float remainingSeconds = Mathf.Max(0f, localVoteEndsAt - Time.realtimeSinceStartup);

        if (timerText != null) timerText.text = $"{Mathf.CeilToInt(remainingSeconds)}s";
    }

    private void HandleVoteUpdated(ClientKickVoteData data)
    {
        root.SetActive(data.HasActiveVote);

        if (!data.HasActiveVote) return;
        
        currentVoteData = data;
        localVoteEndsAt = Time.realtimeSinceStartup + data.RemainingSeconds;
        
        bool isTarget = false;
        
        if (SteamIdentity.TryGetLocalSteamID(out ulong localSteamID))
        {
            isTarget = data.TargetSteamID == localSteamID;
        }

        if (isTarget)
        {
            titleText.text = "A vote has started to remove you";
            progressText.text = "Waiting for the group decision...";
            yesButton.gameObject.SetActive(false);
            noButton.gameObject.SetActive(false);
        }
        else
        {
            titleText.text = $"Remove {data.TargetDisplayName}?";
            progressText.text = $"{data.YesVotes}/{data.RequiredYesVotes} yes votes";
            yesButton.gameObject.SetActive(true);
            noButton.gameObject.SetActive(true);
        }
        
        reasonText.text = data.Reason.ToDisplayText();
        timerText.text = $"{Mathf.CeilToInt(data.RemainingSeconds)}s";
    }

    private void HandleVoteFinished(bool succeeded, string message)
    {
        root.SetActive(false);
    }
}