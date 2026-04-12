using UnityEngine;

public class LevelModTest : MonoBehaviour
{
    [SerializeField] private GameObject variationToToggle;

    private void Awake()
    {
        Debug.Log("LevelModTest Awake");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) variationToToggle.SetActive(!variationToToggle.activeInHierarchy);
    }

    public void Test()
    {

    }
}
