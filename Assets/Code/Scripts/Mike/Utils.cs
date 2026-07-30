using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class Utils
{
    public static void Swap<T>(this IList<T> array, int indexA, int indexB)
    {
        (array[indexB], array[indexA]) = (array[indexA], array[indexB]);
    }
    public static void Shuffle<T>(this IList<T> array)
    {
        for (int i = 0; i < array.Count - 1; i++)
        {
            int r = UnityEngine.Random.Range(i, array.Count);
            (array[r], array[i]) = (array[i], array[r]);
        }
    }
    public static void ToggleChildren(this Transform tr, bool childrenActive)
    {
        foreach(Transform child in tr) child.gameObject.SetActive(childrenActive);
    }
    public static IEnumerable<Transform> GetDirectChildren(this Transform t)
    {
        foreach (Transform c in t)
        {
            if (c.parent == t) yield return c;
        }
    }
    public static void ListenOnce(this UnityEvent handler, System.Action action)
    {
        if (handler == null) return;

        void Wrapper()
        {
            handler.RemoveListener(Wrapper);
            action();
        }

        handler.AddListener(Wrapper);
    }
}