namespace ZFrameWork
{

/// <summary>
/// 任务访问器：外部通过它操作任务（删除 / 暂停 / 继续）。
/// 任务结束或被删除后本访问器被置为未激活，此后所有操作只报错、不生效。
/// 访问器不池化：外部可能长期持有，复用会让旧引用突然指向新任务。
/// </summary>
public class TimeTaskEntry
{
    private TimeTask task;
    private ZTimer owner;
    private bool isActive;

    /// <summary>是否仍然有效（任务未结束、未被删除）。</summary>
    public bool IsActive => isActive;

    /// <summary>关联的任务。</summary>
    internal TimeTask Task => task;

    /// <summary>绑定任务与所属核心：绑定后访问器生效。</summary>
    internal void Bind(TimeTask task, ZTimer owner)
    {
        this.task = task;
        this.owner = owner;
        isActive = true;
    }

    /// <summary>解绑：清空任务与所属核心并置未激活，此后所有操作都会报错。</summary>
    internal void Unbind()
    {
        task = null;
        owner = null;
        isActive = false;
    }

    /// <summary>删除任务。</summary>
    public bool RemoveTask()
    {
        if (!CheckActive("RemoveTask"))
        {
            return false;
        }

        owner.RemoveTask(this);
        return true;
    }

    /// <summary>暂停任务：暂停期间该任务的时间不流逝、不产生追帧。</summary>
    public bool Pause()
    {
        if (!CheckActive("Pause"))
        {
            return false;
        }

        owner.PauseTask(this);
        return true;
    }

    /// <summary>继续任务。</summary>
    public bool Resume()
    {
        if (!CheckActive("Resume"))
        {
            return false;
        }

        owner.ResumeTask(this);
        return true;
    }

    /// <summary>入口守卫：未激活的访问器上的一切操作只记日志并拒绝。</summary>
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
