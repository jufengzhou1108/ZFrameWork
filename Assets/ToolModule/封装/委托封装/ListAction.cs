using System;
using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>基于 List 的泛型 Action 封装。</summary>
    public class ListAction<T>
    {
        private readonly List<Action<T>> _list = new();
        private readonly Dictionary<Action<T>, int> _indices = new();

        public int Count => _list.Count;

        public void Subscribe(Action<T> action)
        {
            if (action == null || _indices.ContainsKey(action)) return;

            _indices[action] = _list.Count;
            _list.Add(action);
        }

        public static ListAction<T> operator +(ListAction<T> listAction, Action<T> action)
        {
            listAction?.Subscribe(action);
            return listAction;
        }

        public bool Unsubscribe(Action<T> action)
        {
            if (action == null || !_indices.TryGetValue(action, out var index)) return false;

            _list.RemoveAt(index);
            _indices.Remove(action);
            for (var i = index; i < _list.Count; i++)
            {
                _indices[_list[i]] = i;
            }

            return true;
        }

        public static ListAction<T> operator -(ListAction<T> listAction, Action<T> action)
        {
            listAction?.Unsubscribe(action);
            return listAction;
        }

        public void Clear()
        {
            _list.Clear();
            _indices.Clear();
        }

        public void Invoke(T arg)
        {
            var visited = HashSetPool<Action<T>>.Get();
            try
            {
                for (var index = 0; index < _list.Count;)
                {
                    var action = _list[index];
                    if (!visited.Add(action))
                    {
                        index++;
                        continue;
                    }

                    try
                    {
                        action?.Invoke(arg);
                    }
                    catch (Exception exception)
                    {
                        ZLog.LogError($"ListAction<{typeof(T).Name}> 回调执行异常：{exception}");
                    }
                    index = 0;
                }
            }
            finally
            {
                HashSetPool<Action<T>>.Release(visited);
            }
        }
    }

    /// <summary>无参数回调的 Action 封装。</summary>
    public class ListAction
    {
        private readonly List<Action> _list = new();
        private readonly Dictionary<Action, int> _indices = new();

        public int Count => _list.Count;

        public void Subscribe(Action action)
        {
            if (action == null || _indices.ContainsKey(action)) return;

            _indices[action] = _list.Count;
            _list.Add(action);
        }

        public static ListAction operator +(ListAction listAction, Action action)
        {
            listAction?.Subscribe(action);
            return listAction;
        }

        public bool Unsubscribe(Action action)
        {
            if (action == null || !_indices.TryGetValue(action, out var index)) return false;

            _list.RemoveAt(index);
            _indices.Remove(action);
            for (var i = index; i < _list.Count; i++)
            {
                _indices[_list[i]] = i;
            }

            return true;
        }

        public static ListAction operator -(ListAction listAction, Action action)
        {
            listAction?.Unsubscribe(action);
            return listAction;
        }

        public void Clear()
        {
            _list.Clear();
            _indices.Clear();
        }

        public void Invoke()
        {
            var visited = HashSetPool<Action>.Get();
            try
            {
                for (var index = 0; index < _list.Count;)
                {
                    var action = _list[index];
                    if (!visited.Add(action))
                    {
                        index++;
                        continue;
                    }

                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception exception)
                    {
                        ZLog.LogError($"ListAction 回调执行异常：{exception}");
                    }
                    index = 0;
                }
            }
            finally
            {
                HashSetPool<Action>.Release(visited);
            }
        }
    }
}
