using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ListExtensions {
    public static IList<T> Swap<T>(this IList<T> list, int indexA, int indexB) {
        T tmp = list[indexA];
        list[indexA] = list[indexB];
        list[indexB] = tmp;
        return list;
    }
}
