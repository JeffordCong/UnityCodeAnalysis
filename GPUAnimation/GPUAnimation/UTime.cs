using System;
using UnityEngine;
public static class UTime
{
#if UNITY_EDITOR
    static UTime() => UnityEditor.EditorApplication.update += EditorTick;
    public static float editorDeltaTime { get; private set; } = 0f;
    public static float editorTime { get; private set; } = 0f;
    static void EditorTick()
    {
        var last = editorTime;
        editorTime = (float)UnityEditor.EditorApplication.timeSinceStartup;
        editorDeltaTime = Mathf.Max(0, (float)(editorTime - last));
    }
#endif

    public static float deltaTime
    {
        get
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return editorDeltaTime;
#endif
            return Time.deltaTime;
        }
    }

    public static float time
    {
        get
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return editorTime;
#endif
            return Time.time;
        }
    }

}

