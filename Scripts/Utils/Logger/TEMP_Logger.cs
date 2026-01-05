using System.Diagnostics;
using UnityEngine;

public static class TEMP_Logger
{
    [Conditional("DEVELOPMENT")]
    public static void Deb(string str)
    {
        UnityEngine.Debug.Log($"[Debug] {str}");
    }

    [Conditional("DEVELOPMENT")]
    public static void Err(string str)
    {
        UnityEngine.Debug.LogError($"[Error] {str}");
    }

    [Conditional("DEVELOPMENT")]
    public static void Wrn(string str)
    {
        UnityEngine.Debug.LogWarning($"[Warning] {str}");
    }

    [Conditional("DEVELOPMENT")]
    public static void Assert(bool condition, string msg)
    {
        UnityEngine.Debug.Assert(condition, msg);
    }
}
