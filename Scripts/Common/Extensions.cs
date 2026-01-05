using UnityEngine;

public static class TransformExtensions
{
    public static void Reset(this Transform transform)
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    public static Vector3 FlatHeight(this Vector3 vector)
    {
        vector.y = 0;
        return vector;
    }
}
