using UnityEngine;

public static class ZLog
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(object message)
    {
#if UNITY_EDITOR
        Debug.Log(message);
#endif
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogWarning(object message)
    {
#if UNITY_EDITOR
        Debug.LogWarning(message);
#endif
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogError(object message)
    {
#if UNITY_EDITOR
        Debug.LogError(message);
#endif
    }
}
