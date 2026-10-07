using System;
using UnityEngine;

namespace ZFrameWork
{

/// <summary>
/// 时间管理器的单例封装：对外唯一入口。
/// 自身负责游戏时间暂停/恢复（timeScale），定时任务部分转发给内部核心 ZTimer。
/// </summary>
public class TimeManager : Singleton<TimeManager>
{
    private readonly ZTimer timer = new ZTimer();

    /// <summary>暂停游戏时间：直接操作 Time.timeScale。</summary>
    public void Pause()
    {
        Time.timeScale = 0;
    }

    /// <summary>恢复游戏时间。</summary>
    public void Play()
    {
        Time.timeScale = 1;
    }

    /// <summary>初始化：开始计时并启动主循环（幂等）。添加任务时也会自动调用。</summary>
    public void Init()
    {
        timer.Init();
    }

    /// <summary>
    /// 添加循环任务。参数含义见 ZTimer.AddTask：interval 间隔毫秒（须 &gt; 0）、action 委托（不能为 null）、
    /// count 次数（&lt;= 0 为无限）、immediately 是否立即执行一次、firstDelay 首次间隔（&lt;= 0 为没有）。
    /// </summary>
    public TimeTaskEntry AddTask(int interval, Action action, int count = 0, bool immediately = true, int firstDelay = -1)
    {
        return timer.AddTask(interval, action, count, immediately, firstDelay);
    }

    /// <summary>当前时间（毫秒，Stopwatch 时基；整体暂停期间不前进）。</summary>
    public long CurrentTime => timer.CurrentTime;

    /// <summary>整体暂停：停住时基并停止推进任务（不影响 timeScale）。</summary>
    public void PauseAll()
    {
        timer.PauseAll();
    }

    /// <summary>整体继续。</summary>
    public void ResumeAll()
    {
        timer.ResumeAll();
    }
}
}
