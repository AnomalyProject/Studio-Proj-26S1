using UnityEngine;
using TMPro;
public class PlayerListUI : MonoBehaviour
{
    [Header("Player List UI")]
    [Space(2)]
    [Tooltip("Player Name text")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("Ready, Not Ready text")]
    [SerializeField] private TMP_Text statusText;
    [Tooltip("is Host badge")]
    [SerializeField] private GameObject hostIndicator;

    public void Setup(ClientPlayerInfo playerInfo)
    {
        nameText.text = playerInfo.DisplayName;
        
        //Ready status colour
        statusText.text = playerInfo.IsReady ? "Ready" : "Not Ready";
        statusText.color = playerInfo.IsReady ? Color.green : Color.red;
        
        //Show/hide Host icon
        hostIndicator.SetActive(playerInfo.IsHost);
    }
}
