using UnityEngine;
using TMPro;
using PurrNet;
using System.ComponentModel;

public class PlayerNameplate : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject visualsParent;

    public string DisplayName = SessionDataController.testName;

    void Start()
    {
        nameText.text = DisplayName;

        if(isOwner) visualsParent.SetActive(false);
    }
}