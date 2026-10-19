using System;
using UnityEngine;

namespace ZFrameWork
{

public class TimeManager : Singleton<TimeManager>
{
    private readonly ZTimer timer = new ZTimer();

    public void Pause()
    {
        Time.timeScale = 0;
    }

    public void Play()
    {
        Time.timeScale = 1;
    }

    public void Init()
    {
        timer.Init();
    }

    public TimeTaskEntry AddTask(int interval, Action action, int count = 0, bool immediately = true, int firstDelay = -1)
    {
        return timer.AddTask(interval, action, count, immediately, firstDelay);
    }

    public long CurrentTime => timer.CurrentTime;

    public void PauseAll()
    {
        timer.PauseAll();
    }

    public void ResumeAll()
    {
        timer.ResumeAll();
    }
}
}
