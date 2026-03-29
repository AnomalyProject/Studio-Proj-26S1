using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using Unity.VectorGraphics;
using UnityEngine.UIElements;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }


    private AsyncOperation async;
    private bool isLoading = false;


    float currentFakeProgress = 0f;


    [Header("Loading logic")]
    [SerializeField] bool showLoadingScreen = true;
    [SerializeField] private bool useRealLoading = true;
    [SerializeField] private float fakeLoadSpeed = 1.0f;
    [SerializeField] private float fakeLoadTime = 3.5f;
    // real loading will show the actual progress of the loading bar
    // fake loading will make will make the progress bar smoothly go up and stop at "99%" which will give the player that 'about to finish' feeling

    [Header("Events")]
    public Action OnLoadStarted;
    public Action<float> OnLoadProgress;
    [SerializeField] bool showRealProgressOnAction = true; //fake progress bar progress will be used during false
    public Action OnLoadFinished;

    [Header("Debug")]
    [SerializeField] bool debugLoadOnStart;
    [SerializeField] string debugSceneToLoadOnStart;
    [SerializeField] bool debugUseAsync;


    // scene validation
    bool SceneExists(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName)
                return true;
        }
        return false;
    }

    bool SceneExists(int sceneIndex)
    {
        return sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings;
    }



    private void Awake()
    {
        // singleton pattern
        if (Instance != null & Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(1); // wait a sec to avoid an infinite loop and freezing
        // debug Mode
        if (debugLoadOnStart)
        {
            if (!debugUseAsync)
            {
                LoadScene(debugSceneToLoadOnStart);
            }
            else
            {
                LoadSceneWithAsync(debugSceneToLoadOnStart);
            }
        }
    }

    public void LoadScene(string sceneName)
    {
        if (!SceneExists(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' does not exist!");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void LoadScene(int sceneIndex)
    {
        if (!SceneExists(sceneIndex))
        {
            Debug.LogError($"Scene '{sceneIndex}' does not exist!");
            return;
        }
        SceneManager.LoadScene(sceneIndex);
    }

    #region async loading overloads
    public void LoadSceneWithAsync(string sceneName)
    {
        if (!SceneExists(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' does not exist!");
            return;
        }
        if (isLoading)
        {
            Debug.LogWarning("Already loading a scene!");
            return;
        }
        StartCoroutine(LoadSceneAsyncEnumerator(sceneName));
    }
    public void LoadSceneWithAsync(int sceneIndex)
    {
        if (!SceneExists(sceneIndex))
        {
            Debug.LogError($"Scene '{sceneIndex}' does not exist!");
            return;
        }
        if (isLoading)
        {
            Debug.LogWarning("Already loading a scene!");
            return;
        }
        StartCoroutine(LoadSceneAsyncEnumerator(sceneIndex));
    }
    public void LoadSceneWithAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (!SceneExists(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' does not exist!");
            return;
        }
        if (isLoading)
        {
            Debug.LogWarning("Already loading a scene!");
            return;
        }
        // Additive & Single load types
        StartCoroutine(LoadSceneAsyncEnumerator(sceneName, mode));
    }
    #endregion


    // If scenes are loaded additively, this removes them.
    public void UnloadScene(string sceneName)
    {
        if (SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            SceneManager.UnloadSceneAsync(sceneName);
        }
    }

    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }


    IEnumerator LoadSceneAsyncEnumerator(string sceneName) => PerformAsyncLoading(SceneManager.LoadSceneAsync(sceneName));
    IEnumerator LoadSceneAsyncEnumerator(int sceneIndex) => PerformAsyncLoading(SceneManager.LoadSceneAsync(sceneIndex));
    IEnumerator LoadSceneAsyncEnumerator(string sceneName, LoadSceneMode mode = LoadSceneMode.Single) => PerformAsyncLoading(SceneManager.LoadSceneAsync(sceneName, mode));
    IEnumerator PerformAsyncLoading(AsyncOperation op)
    {
        // Show loading UI if loading screen exists AND showLoadingScreen is checked
        if (showLoadingScreen)
            LoadingManager.Instance.ShowUI();

        // Start loading scene in background but don't allow to switch yet
        async = op;
        async.allowSceneActivation = false;

        isLoading = true;

        // reset progress
        LoadingManager.Instance.SetProgress(0f);

        //action start
        OnLoadStarted?.Invoke();

        while (!async.isDone)
        {
            if (useRealLoading)
            {
                // progress goes from 0 to 0.9 before activation
                float progress = Mathf.Clamp01(async.progress / 0.9f);
                LoadingManager.Instance.SetProgress(progress);

                // action progress
                OnLoadProgress?.Invoke(progress);

                if (async.progress >= 0.9f)
                {
                    yield return new WaitForSeconds(0.5f); // small delay
                    async.allowSceneActivation = true; // switch scene
                }
            }
            else
            {
                while (currentFakeProgress < 0.9f)
                {
                    float progress = Time.deltaTime * fakeLoadSpeed;
                    currentFakeProgress += progress;
                    LoadingManager.Instance.SetProgress(currentFakeProgress);

                    if (!showRealProgressOnAction)
                    {
                        // action fake progress
                        OnLoadProgress?.Invoke(progress);
                    }

                    yield return new WaitForSeconds(.01f); //tiny delay
                }
                yield return new WaitForSeconds(fakeLoadTime);
                currentFakeProgress = 1f;
                LoadingManager.Instance.SetProgress(1f);
                yield return new WaitForSeconds(0.5f); // small delay
                async.allowSceneActivation = true; // switch scene
            }
        }
        isLoading = false;
        //action finished
        OnLoadFinished?.Invoke();
        LoadingManager.Instance.HideUI();
    }
}
