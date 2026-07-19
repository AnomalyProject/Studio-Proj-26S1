using System.Collections.Generic;
using UnityEngine;

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
}