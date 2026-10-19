namespace ZFrameWork
{

public class TimeTaskEntry
{
    private TimeTask task;
    private ZTimer owner;
    private bool isActive;

    public bool IsActive => isActive;

    internal TimeTask Task => task;

    internal void Bind(TimeTask task, ZTimer owner)
    {
        this.task = task;
        this.owner = owner;
        isActive = true;
    }

    internal void Unbind()
    {
        task = null;
        owner = null;
        isActive = false;
    }

    public bool RemoveTask()
    {
        if (!CheckActive("RemoveTask"))
        {
            return false;
        }

        owner.RemoveTask(this);
        return true;
    }

    public bool Pause()
    {
        if (!CheckActive("Pause"))
        {
            return false;
        }

        owner.PauseTask(this);
        return true;
    }

    public bool Resume()
    {
        if (!CheckActive("Resume"))
        {
            return false;
        }

        owner.ResumeTask(this);
        return true;
    }

    private bool CheckActive(string operation)
    {
        if (isActive && task != null && owner != null)
        {
            return true;
        }

        ZLog.LogError($"[TimeTaskEntry.{operation}] 任务已结束或已被删除，本次操作被忽略。");
        return false;
    }
}
}
