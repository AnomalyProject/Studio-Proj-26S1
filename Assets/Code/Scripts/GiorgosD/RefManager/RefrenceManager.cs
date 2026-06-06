using UnityEngine;

public class RefrenceManager : MonoBehaviour
{
    public static RefrenceManager Instance { get; private set; }

    [Header("Refrence Containers")]
    [SerializeField] private GlobalContainer global;
    [SerializeField] private MainMenuContainer menu;
    [SerializeField] private GamePlayContainer gameplay;

    public GlobalContainer Global => global;
    public MainMenuContainer Menu => menu;
    public GamePlayContainer Gameplay => gameplay;
    public static SaveData CurrentSave { get; private set; } 

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void LoadLastSave() => CurrentSave = SaveSystem.LoadLast();
}
