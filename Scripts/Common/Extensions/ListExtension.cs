using System;
using System.Collections.Generic;

public static class ListExtensions
{
    private static readonly Random Rng = new Random();

    public static void Resize<T>(this List<T> list, int newSize)
    {
        if (newSize == list.Count)
        {
            return;
        }

        if (newSize > list.Count)
        {
            int elementsToAdd = newSize - list.Count;
            List<T> newElements = new List<T>(elementsToAdd);
            for (int i = 0; i < elementsToAdd; i++)
            {
                newElements.Add(default(T));
            }

            list.AddRange(newElements);
        }
        else
        {
            int indexToRemove = newSize;
            int countToRemove = list.Count - newSize;

            list.RemoveRange(indexToRemove, countToRemove);
        }
    }

    public static void Shuffle<T>(this IList<T> list)
    {
        if (list == null || list.Count < 2)
        {
            return;
        }

        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Rng.Next(n + 1);

            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    // 순서 중요하지 않을때 이 방식으로 remove 하는게 성능상 유리함.
    // 내부 배열 재조정 없게끔 처리
    public static void RemoveSwapBack<T>(this List<T> list, int idx)
    {
        int lastIdx = list.Count - 1;

        if (idx != list.Count - 1)
        {
            list[idx] = list[lastIdx];
        }

        list.RemoveAt(lastIdx);
    }
}
