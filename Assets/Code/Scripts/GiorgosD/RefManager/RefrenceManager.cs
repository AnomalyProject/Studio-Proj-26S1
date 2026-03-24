using UnityEngine;

public class RefrenceManager : MonoBehaviour
{
    public static RefrenceManager Instance { get; private set; }

    [Header("Refrence Containers")]
    [SerializeField] private GlobalContainer globalRef;
    [SerializeField] private MainMenuContainer mainMenuRef;
    [SerializeField] private GamePlayContainer gamePlayRef;

    public GlobalContainer GlobalRef => globalRef;
    public MainMenuContainer MainMenuRef => mainMenuRef;
    public GamePlayContainer GamePlayRef => gamePlayRef;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
