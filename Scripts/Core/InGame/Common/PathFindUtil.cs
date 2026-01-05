using System.Collections.Generic;
using UnityEngine;

public static class PathFindUtil
{
    public static float GetTotalDistance(List<Vector3> paths)
    {
        float distance = 0f;
        for (int i = 0; i < paths.Count; i++)
        {
            if (i + 1 < paths.Count)
            {
                distance += Vector3.Distance(paths[i], paths[i + 1]);
            }
        }
        return distance;
    }

    public static float GetTotalDistanceSqr(List<Vector3> paths)
    {
        float distanceSqr = 0f;
        for (int i = 0; i < paths.Count; i++)
        {
            if (i + 1 < paths.Count)
            {
                distanceSqr += Vector3.SqrMagnitude(paths[i + 1] - paths[i]);
            }
        }
        return distanceSqr;
    }

    // 가장 가까운 Path 를 가진 순으로 정렬
    public static void SortByDistance(List<List<Vector3>> paths)
    {
        Dictionary<List<Vector3>, float> distances = new Dictionary<List<Vector3>, float>();
        foreach (var path in paths)
        {
            distances.Add(path, GetTotalDistanceSqr(path));
        }

        paths.Sort((lhs, rhs) => distances[lhs].CompareTo(distances[rhs]));
    }
}
