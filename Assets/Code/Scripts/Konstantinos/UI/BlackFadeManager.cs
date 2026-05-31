using System.Collections;
using UnityEngine;

public class BlackFadeManager : MonoBehaviour
{
    public static BlackFadeManager Instance;

    public Animator anim;

    [SerializeField] float transitionTime = 1.0f;


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
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

    IEnumerator FadeInAndOut()
    {
        anim.SetTrigger("Fade In");
        yield return new WaitForSeconds(transitionTime);
        anim.SetTrigger("Fade Out");
    }
}
