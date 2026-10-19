using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ZFrameWork
{

public class ZTimer
{
    private const int LoopIntervalMilliseconds = 20;

    private readonly Stopwatch stopwatch = new Stopwatch();
    private readonly Pool<TimeTask> taskPool = new Pool<TimeTask>(() => new TimeTask(), null, task => task.Reset());
    private readonly Dictionary<TimeTask, TimeTaskEntry> entries = new Dictionary<TimeTask, TimeTaskEntry>();

    private readonly List<TimeTask> tasks = new List<TimeTask>(16);
    private bool isInitialized;
    private bool isGlobalPaused;
    private CancellationTokenSource loopCts;

    public long CurrentTime => stopwatch.ElapsedMilliseconds;

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

    public void PauseAll()
    {
        if (isGlobalPaused)
        {
            return;
        }

        isGlobalPaused = true;
        stopwatch.Stop();
    }

    public void ResumeAll()
    {
        if (!isGlobalPaused)
        {
            return;
        }

        isGlobalPaused = false;
        stopwatch.Start();
    }

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

    internal void PauseTask(TimeTaskEntry entry)
    {
        if (entry.Task == null)
        {
            ZLog.LogError("[ZTimer.PauseTask] 访问器没有关联任务，暂停被忽略。");
            return;
        }

        entry.Task.Pause(CurrentTime);
    }

    internal void ResumeTask(TimeTaskEntry entry)
    {
        if (entry.Task == null)
        {
            ZLog.LogError("[ZTimer.ResumeTask] 访问器没有关联任务，继续被忽略。");
            return;
        }

        entry.Task.Resume(CurrentTime);
    }

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
        }
        catch (Exception e)
        {
            ZLog.LogError($"[ZTimer.MainLoop] 主循环异常退出：{e}");
        }
    }

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
