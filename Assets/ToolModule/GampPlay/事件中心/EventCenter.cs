using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZFrameWork
{

public class EventCenter : Singleton<EventCenter>
{
    private Dictionary<Type, IEventContainer> eventDic = new();

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

    public void EventTrigger<T>(T args) where T : struct
    {
        Type key = typeof(T);

        if (!eventDic.ContainsKey(key))
        {
            return;
        }

        (eventDic[key] as EventContainer<T>).action.Invoke(args);
    }

    private interface IEventContainer { }

    private class EventContainer<T> : IEventContainer where T : struct
    {
        public readonly ListAction<T> action = new();
    }

    public void Clear()
    {
        eventDic.Clear();
    }
}
}
