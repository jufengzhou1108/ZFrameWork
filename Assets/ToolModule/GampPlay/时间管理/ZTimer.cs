using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ZFrameWork
{

/// <summary>
/// 定时任务的核心（对外由 TimeManager 单例封装持有）。
/// 单时基（Stopwatch）+ 单循环（UniTask 间隔 20ms）驱动全部任务；任务存 List、删除用交换删除法、任务对象走池。
/// 主循环按游戏时间推进（Time.deltaTime），与 TimeManager 的 timeScale 暂停天然一致。
/// </summary>
public class ZTimer
{
    /// <summary>主循环推进间隔（毫秒）。</summary>
    private const int LoopIntervalMilliseconds = 20;

    private readonly Stopwatch stopwatch = new Stopwatch();
    private readonly Pool<TimeTask> taskPool = new Pool<TimeTask>(() => new TimeTask(), null, task => task.Reset());
    private readonly Dictionary<TimeTask, TimeTaskEntry> entries = new Dictionary<TimeTask, TimeTaskEntry>();

    private readonly List<TimeTask> tasks = new List<TimeTask>(16);
    private bool isInitialized;
    private bool isGlobalPaused;
    private CancellationTokenSource loopCts;

    /// <summary>当前时间（毫秒，Stopwatch 时基；整体暂停期间不前进）。</summary>
    public long CurrentTime => stopwatch.ElapsedMilliseconds;

    /// <summary>初始化：开始计时并启动主循环（幂等）。外部希望提前开始计时可以主动调用；添加任务时也会自动调用。</summary>
    public void Init()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        stopwatch.Restart();
        loopCts = new CancellationTokenSource();
        MainLoop(loopCts.Token).Forget();
    }

    /// <summary>
    /// 添加循环任务。
    /// </summary>
    /// <param name="interval">间隔毫秒，必须大于 0。</param>
    /// <param name="action">任务委托，不能为 null。</param>
    /// <param name="count">执行次数；&lt;= 0 表示无限次。</param>
    /// <param name="immediately">是否立即执行一次（默认 true）；立即执行会作废 firstDelay。</param>
    /// <param name="firstDelay">首次间隔毫秒；&lt;= 0 表示没有。只有 immediately 为 false 时才有意义。</param>
    /// <returns>任务访问器；入参非法时返回 null。</returns>
    public TimeTaskEntry AddTask(int interval, Action action, int count = 0, bool immediately = true, int firstDelay = -1)
    {
        if (interval <= 0)
        {
            ZLog.LogError($"[ZTimer.AddTask] interval 必须大于 0，当前为 {interval}，添加被忽略。");
            return null;
        }

        if (action == null)
        {
            ZLog.LogError("[ZTimer.AddTask] action 不能为 null，添加被忽略。");
            return null;
        }

        //未初始化就添加任务：自动开始计时，避免 lastTime 基于未启动的时基
        Init();

        if (firstDelay <= 0)
        {
            firstDelay = -1;
        }

        TimeTask task = taskPool.Get();
        if (task == null)
        {
            ZLog.LogError("[ZTimer.AddTask] 任务池取出失败，添加被忽略。");
            return null;
        }

        task.Init(interval, action, count <= 0, count, CurrentTime, firstDelay);

        TimeTaskEntry entry = new TimeTaskEntry();
        entry.Bind(task, this);
        tasks.Add(task);
        entries[task] = entry;

        if (immediately)
        {
            //先登记再立即执行：回调抛异常也不会让任务游离在管理之外
            try
            {
                task.ExecuteImmediately();
            }
            catch (Exception e)
            {
                ZLog.LogError($"[ZTimer.AddTask] 首次立即执行异常：{e}");
            }
        }

        return entry;
    }

    /// <summary>整体暂停：停住时基并停止推进任务；恢复后不补帧。</summary>
    public void PauseAll()
    {
        if (isGlobalPaused)
        {
            return;
        }

        isGlobalPaused = true;
        stopwatch.Stop();
    }

    /// <summary>整体继续。</summary>
    public void ResumeAll()
    {
        if (!isGlobalPaused)
        {
            return;
        }

        isGlobalPaused = false;
        stopwatch.Start();
    }

    /// <summary>删除任务（由访问器调用）。</summary>
    internal void RemoveTask(TimeTaskEntry entry)
    {
        TimeTask task = entry.Task;
        if (task == null)
        {
            ZLog.LogError("[ZTimer.RemoveTask] 访问器没有关联任务，仅解绑访问器。");
            entry.Unbind();
            return;
        }

        int index = tasks.IndexOf(task);
        if (index < 0)
        {
            ZLog.LogError("[ZTimer.RemoveTask] 任务不在列表中，仅解绑访问器。");
            entry.Unbind();
            return;
        }

        RemoveAt(index);
    }

    /// <summary>暂停任务（由访问器调用）。</summary>
    internal void PauseTask(TimeTaskEntry entry)
    {
        if (entry.Task == null)
        {
            ZLog.LogError("[ZTimer.PauseTask] 访问器没有关联任务，暂停被忽略。");
            return;
        }

        entry.Task.Pause(CurrentTime);
    }

    /// <summary>继续任务（由访问器调用）。</summary>
    internal void ResumeTask(TimeTaskEntry entry)
    {
        if (entry.Task == null)
        {
            ZLog.LogError("[ZTimer.ResumeTask] 访问器没有关联任务，继续被忽略。");
            return;
        }

        entry.Task.Resume(CurrentTime);
    }

    /// <summary>主循环：每 20ms 醒来一次处理任务列表。</summary>
    private async UniTaskVoid MainLoop(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await UniTask.Delay(LoopIntervalMilliseconds, DelayType.DeltaTime, cancellationToken: ct);

                if (isGlobalPaused)
                {
                    continue;
                }

                ProcessTasks();
            }
        }
        catch (OperationCanceledException)
        {
            //停止时正常退出
        }
        catch (Exception e)
        {
            ZLog.LogError($"[ZTimer.MainLoop] 主循环异常退出：{e}");
        }
    }

    /// <summary>处理一轮：先清理次数已耗尽的任务，再按到期时刻推进其余任务。</summary>
    private void ProcessTasks()
    {
        //循环执行前检测：次数为 0 的任务在这里销毁（倒序遍历，配合交换删除不会跳过元素）
        for (int i = tasks.Count - 1; i >= 0; i--)
        {
            if (tasks[i].IsFinished)
            {
                RemoveAt(i);
            }
        }

        long nowTime = CurrentTime;

        //任务委托内不得操作管理器（约定）；逐任务隔离异常，单个任务抛异常不影响其它任务
        for (int i = 0; i < tasks.Count; i++)
        {
            try
            {
                tasks[i].Tick(nowTime);
            }
            catch (Exception e)
            {
                ZLog.LogError($"[ZTimer.ProcessTasks] 任务执行异常，跳过该任务本轮剩余推进：{e}");
            }
        }
    }

    /// <summary>交换删除法：把尾元素挪到被删位置再 RemoveAt 末尾，O(1)；随后解绑访问器、清字典项、归还池。</summary>
    private void RemoveAt(int index)
    {
        TimeTask task = tasks[index];
        int last = tasks.Count - 1;
        if (index != last)
        {
            tasks[index] = tasks[last];
        }
        tasks.RemoveAt(last);

        if (entries.TryGetValue(task, out TimeTaskEntry entry))
        {
            entry.Unbind();
            entries.Remove(task);
        }

        taskPool.Add(task);
    }
}
}
