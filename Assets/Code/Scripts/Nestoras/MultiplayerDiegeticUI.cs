using UnityEngine;

public class MultiplayerDiegeticUI : MonoBehaviour
{
    [SerializeField] private SimpleInteractable invite;
    [SerializeField] private SimpleInteractable[] maxPlayerOptions;

    private void Start()
    {
        //invite.OnInteracted.AddListener(SessionUIRoot.Instance.LobbyUI.OnInviteClicked);
        for (int i = 0; i < maxPlayerOptions.Length; i++)
        {
            int maxPlayers = i + 2;
            maxPlayerOptions[i].OnInteracted.AddListener(() => SessionUIRoot.Instance.LobbyUI.RequestMaxPlayers(maxPlayers));
        }
    }
}
