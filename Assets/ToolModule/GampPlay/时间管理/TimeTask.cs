using System;

namespace ZFrameWork
{

public class TimeTask
{
    private const int CatchUpBudgetMilliseconds = 60;

    private Action action;
    private int interval;
    private long lastTime;
    private int firstDelay;
    private int remainingCount;
    private bool isInfinite;
    private bool isPaused;
    private long pausedElapsed;

    internal bool IsFinished => !isInfinite && remainingCount <= 0;

    internal void Init(int interval, Action action, bool isInfinite, int count, long nowTime, int firstDelay)
    {
        this.interval = interval;
        this.action = action;
        this.isInfinite = isInfinite;
        this.remainingCount = isInfinite ? 1 : count;
        this.firstDelay = firstDelay;
        this.lastTime = nowTime;
        this.isPaused = false;
        this.pausedElapsed = 0;
    }

    internal void Reset()
    {
        action = null;
    }

    internal void ExecuteImmediately()
    {
        firstDelay = -1;
        Invoke();
    }

    internal void Tick(long nowTime)
    {
        if (isPaused || IsFinished)
        {
            return;
        }

        int maxTicks = CatchUpBudgetMilliseconds / interval;
        if (maxTicks < 1)
        {
            maxTicks = 1;
        }

        for (int i = 0; i < maxTicks; i++)
        {
            //首次触发用 firstDelay（>0 时），之后回到正常间隔
            long due = firstDelay > 0 ? firstDelay : interval;
            if (nowTime - lastTime < due)
            {
                break;
            }

            lastTime += due;
            if (firstDelay > 0)
            {
                firstDelay = -1;
            }

            Invoke();

            if (IsFinished)
            {
                break;
            }
        }
    }

    internal void Pause(long nowTime)
    {
        if (isPaused)
        {
            return;
        }

        pausedElapsed = nowTime - lastTime;
        lastTime = nowTime;
        isPaused = true;
    }

    internal void Resume(long nowTime)
    {
        if (!isPaused)
        {
            return;
        }

        lastTime = nowTime - pausedElapsed;
        isPaused = false;
        pausedElapsed = 0;
    }

    private void Invoke()
    {
        if (!isInfinite)
        {
            remainingCount--;
        }

        action?.Invoke();
    }
}
}
