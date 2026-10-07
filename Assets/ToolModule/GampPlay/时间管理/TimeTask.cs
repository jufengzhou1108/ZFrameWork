using System;

namespace ZFrameWork
{

/// <summary>
/// 定时任务。由 ZTimer 池化复用：除委托外的字段每次从池中取出时都会被重新初始化，
/// 因此归还池中时只需置空委托（打断对回调目标的强引用）。
/// </summary>
public class TimeTask
{
    /// <summary>单轮最多补跑的时间预算（毫秒）：一次循环最多补 60ms 的量，避免高强度追帧。</summary>
    private const int CatchUpBudgetMilliseconds = 60;

    private Action action;
    private int interval;
    private long lastTime;
    private int firstDelay;
    private int remainingCount;
    private bool isInfinite;
    private bool isPaused;
    private long pausedElapsed;

    /// <summary>次数是否耗尽（只有有限次任务会耗尽）。</summary>
    internal bool IsFinished => !isInfinite && remainingCount <= 0;

    /// <summary>从池中取出时初始化；nowTime 为加入任务时的管理器时间（毫秒）。</summary>
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

    /// <summary>归还池中时置空：只清委托，其余字段由下次 Init 覆盖。</summary>
    internal void Reset()
    {
        action = null;
    }

    /// <summary>立即执行一次（不消耗时间轴）；"立即执行"会作废首次间隔。</summary>
    internal void ExecuteImmediately()
    {
        firstDelay = -1;
        Invoke();
    }

    /// <summary>按到期时刻推进：逐间隔执行，单轮最多补（60ms / 间隔，最小为 1）次。</summary>
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

    /// <summary>暂停：记下已过的时间；恢复时据此平移 lastTime，使暂停期间"时间不流逝"、不产生追帧。</summary>
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

    /// <summary>恢复：把 lastTime 平移到"已过时间"之后，暂停时长不计入、不补帧。</summary>
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

    /// <summary>执行一次：先扣次数再回调，回调抛异常也不会重复触发同一次。</summary>
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
