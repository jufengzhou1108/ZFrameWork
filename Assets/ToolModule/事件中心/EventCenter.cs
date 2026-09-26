using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 事件中心模块：以事件参数类型为键的全局事件总线，同一事件的参数类型固定（T 为参数结构体）。
/// </summary>
namespace ZFrameWork
{

public class EventCenter : Singleton<EventCenter>
{
    private Dictionary<Type, IEventContainer> eventDic = new();

    /// <summary>
    /// 添加监听
    /// </summary>
    /// <param name="action">回调函数</param>
    public void AddListener<T>(Action<T> action) where T : struct
    {
        Type key = typeof(T);
        if (!eventDic.ContainsKey(key))
        {
            EventContainer<T> container = new EventContainer<T>();
            container.action.Subscribe(action);

            eventDic.Add(key, container);
            return;
        }

        (eventDic[key] as EventContainer<T>).action.Subscribe(action);
    }

    /// <summary>
    /// 移除监听
    /// </summary>
    /// <param name="action">回调函数</param>
    public void RemoveListener<T>(Action<T> action) where T : struct
    {
        Type key = typeof(T);

        if (!eventDic.ContainsKey(key))
        {
            return;
        }

        EventContainer<T> container = eventDic[key] as EventContainer<T>;
        container.action.Unsubscribe(action);

        if (container.action.Count == 0)
        {
            eventDic.Remove(key);
        }
    }

    /// <summary>
    /// 触发事件
    /// </summary>
    /// <param name="args">事件参数</param>
    public void EventTrigger<T>(T args) where T : struct
    {
        Type key = typeof(T);

        if (!eventDic.ContainsKey(key))
        {
            return;
        }

        (eventDic[key] as EventContainer<T>).action.Invoke(args);
    }

    // 事件容器接口
    private interface IEventContainer { }

    // 事件容器类（T 是参数结构体）
    private class EventContainer<T> : IEventContainer where T : struct
    {
        public readonly ListAction<T> action = new();
    }

    /// <summary>
    /// 清空事件中心
    /// </summary>
    public void Clear()
    {
        eventDic.Clear();
    }
}
}
