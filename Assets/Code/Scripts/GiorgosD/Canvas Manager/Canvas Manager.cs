using System.Collections.Generic;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    public static CanvasManager Instance { get; private set; }

    public enum CanvasID
    {
        MainMenu,
        PauseMenu,
        Settings,
        HUD,
        //Inventory,
        //VendingMachine,
        DescriptionPrompt
    }

    [System.Serializable]
    private class CanvasData
    {
        [SerializeField] private CanvasID canvasID;
        [SerializeField] private GameObject canvasObject;
        [SerializeField] private bool isPersistent;

        public CanvasID ID => canvasID;
        public GameObject CanvasObject => canvasObject;
        public bool IsPersistent => isPersistent;
    }

    // TODO: Register inventory and vending machine UI canvases here when those tasks are created and ready.
    [SerializeField] private List<CanvasData> canvas = new List<CanvasData>();

    #region Initialization
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);


        CanvasInitializtion();
    }

    /// <summary>
    /// Closes all canvases on awake and leaves only the persistent ones open.
    /// </summary>
    private void CanvasInitializtion()
    {
        foreach (var entry in canvas)
        {
            if (entry.CanvasObject != null)
            {
                entry.CanvasObject.SetActive(entry.IsPersistent);
            }
        }
    }
    #endregion

    #region Show
    /// <summary>
    /// Shows canvas with the given ID.
    /// </summary>
    /// <param name="id"></param>
    public void Show(CanvasID id)
    {
        var entry = GetEntry(id);
        if (entry != null)
        {
            entry.CanvasObject.SetActive(true);
        }
    }
    #endregion

    #region Hide
    /// <summary>
    /// Hides canvas with the given ID unless it's persistant.
    /// </summary>
    /// <param name="id"></param>
    public void Hide(CanvasID id)
    {
        var entry = GetEntry(id);
        if (entry != null && !entry.IsPersistent)
        {
            entry.CanvasObject.SetActive(false);
        }
    }
    #endregion

    #region Show Only
    /// <summary>
    /// Shows only the requested canvas and hides all the others except the persistent ones.
    /// </summary>
    /// <param name="id"></param>
    public void ShowOnly(CanvasID id)
    {
        foreach (var entry in canvas)
        {
            if (entry.IsPersistent)
            {
                continue;
            }

            if (entry.ID == id)
            {
                entry.CanvasObject.SetActive(true);
            }
            else
            {
                entry.CanvasObject.SetActive(false);
            }
        }

        if (GetEntry(id) == null)
        {
            Debug.LogWarning($"Canvas Manager: There is no canvas with id {id} so it's either null or not registered");
        }
    }
    #endregion

    #region Hide All
    /// <summary>
    /// Hides all canvases except the persistent ones.
    /// </summary>
    public void HideAll()
    {
        foreach (var entry in canvas)
        {
            if (!entry.IsPersistent)
            {
                entry.CanvasObject.SetActive(false);
            }
        }   
    }
    #endregion

    #region Helper Functions
    /// <summary>
    /// Helper Function to get the canvas data entry for a given canvas ID.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private CanvasData GetEntry(CanvasID id)
    {
        var entry = canvas.Find(e => e.ID == id);
        if (entry == null || entry.CanvasObject == null)
        {
            Debug.LogWarning($"Canvas Manager: There is no canvas with id {id} so it's either null or not registered");
            return null;
        }
        return entry;
    }
    #endregion
}
