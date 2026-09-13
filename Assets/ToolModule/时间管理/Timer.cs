using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;

namespace ZFrameWork
{

public class UnityTimer 
{
    private long nextTriggerMilliseconds;
    private Action action;
    private Stopwatch stopwatch = new();
    private bool isRunning = false;
    private int intervalTime;

    //创建循环任务
    public void StartRepeatTimer(int intervalTime, Action action)
    {
        if (intervalTime <= 0)
        {
            ZLog.LogError("[UnityTimer.StartRepeatTimer] intervalTime 必须大于 0");
            return;
        }

        if (action == null)
        {
            ZLog.LogError("[UnityTimer.StartRepeatTimer] action 不能为 null");
            return;
        }

        //先重置计时器
        Reset();

        this.action = action;
        nextTriggerMilliseconds = 0;
        isRunning = true;

        action?.Invoke();
        nextTriggerMilliseconds += intervalTime;
        this.intervalTime = intervalTime;
        stopwatch.Start();

        PublicMono.Instance.StartCoroutine( LoopTick());
    }

    //轮询计时，唤醒后追帧（最多追 100 帧，避免死循环）
    private IEnumerator LoopTick()
    {
        while (isRunning)
        {
            yield return new WaitForSecondsRealtime(intervalTime / 1000f);
            int catchUp = 100;
            while (isRunning && stopwatch.ElapsedMilliseconds >= nextTriggerMilliseconds && --catchUp >= 0)
            {
                Tick();
            }
            // 追不上就放弃，等下一轮
            if (catchUp < 0)
            {
                nextTriggerMilliseconds = stopwatch.ElapsedMilliseconds + intervalTime;
            }
        }
    }

    //单次触发
    private void Tick()
    {
        action?.Invoke();
        nextTriggerMilliseconds += intervalTime;
    }


    public void End()
    {
        isRunning = false;
        stopwatch.Stop();
    }

    /// <summary>
    /// 单次定时器，延迟 delayMs 毫秒后执行一次回调。
    /// </summary>
    public void StartOnceTimer(int delayMs, Action action)
    {
        if (delayMs <= 0)
        {
            ZLog.LogError("[UnityTimer.StartOnceTimer] delayMs 必须大于 0");
            return;
        }

        if (action == null)
        {
            ZLog.LogError("[UnityTimer.StartOnceTimer] action 不能为 null");
            return;
        }

        Reset();
        this.action = action;
        intervalTime = delayMs;
        isRunning = true;
        stopwatch.Start();
        PublicMono.Instance.StartCoroutine(LoopOnceTick());
    }

    private IEnumerator LoopOnceTick()
    {
        yield return new WaitForSecondsRealtime(intervalTime / 1000f);
        if (isRunning)
        {
            Tick();
            End();
        }
    }

    private void Reset()
    {
        nextTriggerMilliseconds = 0;
        action = null;
        stopwatch.Reset();
        isRunning = false;
    }
}
}
