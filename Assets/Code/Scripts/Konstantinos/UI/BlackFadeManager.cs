using System.Collections;
using UnityEngine;

public class BlackFadeManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    public static void ResetAnimatorReference() => anim = null;

    public static BlackFadeManager Instance;
    static private Animator anim;

    [SerializeField] private float transitionTime = 1.0f;
    public float TransitionTime => transitionTime;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        anim = GetComponentInChildren<Animator>();

        DontDestroyOnLoad(gameObject);

        PlayerBody.OnLocalPlayerSpawned += playerBody => FadeOut();
    }

    public void FadeIn()
    {
        anim.SetTrigger("Fade In");
    }

    public void FadeOut()
    {
        anim.SetTrigger("Fade Out");
    }

    public void FullFade()
    {
        StartCoroutine(FadeInAndOut());
    }

    private IEnumerator FadeInAndOut()
    {
        anim.SetTrigger("Fade In");
        yield return new WaitForSeconds(transitionTime);
        anim.SetTrigger("Fade Out");
    }
}
