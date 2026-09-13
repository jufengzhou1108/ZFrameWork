using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �¼����ģ�����ͬ�¼��Ķ��ĺʹ�����Ĭ��һ���¼��Ĳ����ǹ̶���
/// </summary>
namespace ZFrameWork
{

public class EventCenter : Singleton<EventCenter>
{
    private Dictionary<Type, IEventContainer> eventDic = new();

    /// <summary>
    /// ���Ӽ���
    /// </summary>
    /// <param name="eventName">�����¼���</param>
    /// <param name="action">�ص�����</param>
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
    /// �Ƴ�����
    /// </summary>
    /// <param name="eventName">�����¼���</param>
    /// <param name="action">�ص�����</param>
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
    /// �����¼�
    /// </summary>
    /// <param name="eventName">�¼���</param>
    public void EventTrigger<T>(T args) where T : struct
    {
        Type key = typeof(T);

        if (!eventDic.ContainsKey(key))
        {
            return;
        }

        (eventDic[key] as EventContainer<T>).action.Invoke(args);
    }

    //�¼���������
    private interface IEventContainer { }

    //�¼�������(T�ǲ����ṹ��)
    private class EventContainer<T> : IEventContainer where T : struct
    {
        public readonly ListAction<T> action = new();
    }

    /// <summary>
    /// ����¼�����
    /// </summary>
    public void Clear()
    {
        eventDic.Clear();
    }
}
}
