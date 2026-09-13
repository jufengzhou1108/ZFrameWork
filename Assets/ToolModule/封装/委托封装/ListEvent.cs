using System;
using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>无参数事件封装，支持去重和安全触发。</summary>
    public sealed class ListEvent
    {
        private readonly List<Action> _callbacks = new();
        private readonly Dictionary<Action, int> _indices = new();

        public int Count => _callbacks.Count;

        public event Action Invoked
        {
            add => Subscribe(value);
            remove => Unsubscribe(value);
        }

        private void Subscribe(Action callback)
        {
            if (callback == null || _indices.ContainsKey(callback)) return;

            _indices[callback] = _callbacks.Count;
            _callbacks.Add(callback);
        }

        private bool Unsubscribe(Action callback)
        {
            if (callback == null || !_indices.TryGetValue(callback, out var index)) return false;

            _callbacks.RemoveAt(index);
            _indices.Remove(callback);
            UpdateIndices(index);
            return true;
        }

        public void Invoke()
        {
            var visited = HashSetPool<Action>.Get();
            for (var index = 0; index < _callbacks.Count;)
            {
                var callback = _callbacks[index];
                if (!visited.Add(callback))
                {
                    index++;
                    continue;
                }

                callback?.Invoke();
                index = 0;
            }
            HashSetPool<Action>.Release(visited);
        }

        public void Clear()
        {
            _callbacks.Clear();
            _indices.Clear();
        }

        private void UpdateIndices(int startIndex)
        {
            for (var i = startIndex; i < _callbacks.Count; i++)
            {
                _indices[_callbacks[i]] = i;
            }
        }
    }

    /// <summary>单参数事件封装，支持去重和安全触发。</summary>
    public sealed class ListEvent<T>
    {
        private readonly List<Action<T>> _callbacks = new();
        private readonly Dictionary<Action<T>, int> _indices = new();

        public int Count => _callbacks.Count;

        public event Action<T> Invoked
        {
            add => Subscribe(value);
            remove => Unsubscribe(value);
        }

        private void Subscribe(Action<T> callback)
        {
            if (callback == null || _indices.ContainsKey(callback)) return;

            _indices[callback] = _callbacks.Count;
            _callbacks.Add(callback);
        }

        private bool Unsubscribe(Action<T> callback)
        {
            if (callback == null || !_indices.TryGetValue(callback, out var index)) return false;

            _callbacks.RemoveAt(index);
            _indices.Remove(callback);
            UpdateIndices(index);
            return true;
        }

        public void Invoke(T arg)
        {
            var visited = HashSetPool<Action<T>>.Get();
            for (var index = 0; index < _callbacks.Count;)
            {
                var callback = _callbacks[index];
                if (!visited.Add(callback))
                {
                    index++;
                    continue;
                }

                callback?.Invoke(arg);
                index = 0;
            }
            HashSetPool<Action<T>>.Release(visited);
        }

        public void Clear()
        {
            _callbacks.Clear();
            _indices.Clear();
        }

        private void UpdateIndices(int startIndex)
        {
            for (var i = startIndex; i < _callbacks.Count; i++)
            {
                _indices[_callbacks[i]] = i;
            }
        }
    }
}
