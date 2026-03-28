using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using static UnityEngine.Rendering.HDROutputUtils;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }


    private AsyncOperation async;

    [Header("Loading UI")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Slider progressBar;
    [SerializeField] private bool useRealLoading = true;
    [SerializeField] private float fakeLoadSpeed = 1.0f;
    [SerializeField] private float fakeLoadTime = 3.5f;
    // real loading will show the actual progress of the loading bar
    // fake loading will make will make the progress bar smoothly go up and stop at "99%" which will give the player that 'about to finish' feeling


    [Header("Debug")]
    [SerializeField] bool debugLoadOnStart;
    [SerializeField] string debugSceneToLoadOnStart;
    [SerializeField] bool debugUseAsync;

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
        SceneManager.LoadScene(sceneName);
    }

    public void LoadScene(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

    public void LoadSceneWithAsync(string sceneName)
    {
        StartCoroutine(LoadSceneAsyncEnumerator(sceneName));
    }
    public void LoadSceneWithAsync(int sceneIndex)
    {
        StartCoroutine(LoadSceneAsyncEnumerator(sceneIndex));
    }

    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }


    IEnumerator LoadSceneAsyncEnumerator(string sceneName) => PerformAsyncLoading(SceneManager.LoadSceneAsync(sceneName));
    IEnumerator LoadSceneAsyncEnumerator(int sceneIndex) => PerformAsyncLoading(SceneManager.LoadSceneAsync(sceneIndex));
    IEnumerator PerformAsyncLoading(AsyncOperation op)
    {
        // Show loading UI
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        // Start loading scene in background but don't allow to switch yet
        async = op;
        async.allowSceneActivation = false;

        while (!async.isDone)
        {
            if (useRealLoading)
            {
                // progress goes from 0 to 0.9 before activation
                float progress = Mathf.Clamp01(async.progress / 0.9f);
                progressBar.value = progress;

                if (async.progress >= 0.9f)
                {
                    yield return new WaitForSeconds(0.5f); // small delay
                    async.allowSceneActivation = true; // switch scene
                }
            }
            else
            {
                while (progressBar.value < progressBar.maxValue - progressBar.maxValue / 9)
                {
                    float progress = progressBar.value += Time.deltaTime * fakeLoadSpeed;
                    progressBar.value = progress;
                    yield return new WaitForSeconds(.01f); //tiny delay
                }
                yield return new WaitForSeconds(fakeLoadTime);
                progressBar.value = progressBar.maxValue;
                yield return new WaitForSeconds(0.5f); // small delay
                async.allowSceneActivation = true; // switch scene
            }

            if (loadingScreen != null) loadingScreen.SetActive(false);
        }
    }
}
