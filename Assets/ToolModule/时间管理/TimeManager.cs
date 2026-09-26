using UnityEngine;

namespace ZFrameWork
{

/// <summary>时间控制：暂停与恢复，直接操作 Time.timeScale。</summary>
public static class TimeManager
{
    public static void Pause()
    {
        Time.timeScale = 0;
    }

    public static void Play()
    {
        Time.timeScale = 1;
    }
}
}
