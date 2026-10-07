using System;
using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>在内容变化后向订阅者传播当前只读列表。</summary>
    public sealed class ReactiveList<T>
    {
        private readonly List<T> _items;
        private readonly ListAction<IReadOnlyList<T>> _callbacks = new();

        public int Count => _items.Count;
        public ReactiveList()
        {
            _items = new List<T>();
        }

        public ReactiveList(IEnumerable<T> items)
        {
            _items = items == null ? new List<T>() : new List<T>(items);
        }

        public void Add(T item)
        {
            _items.Add(item);
            NotifyChanged();
        }

        public bool Remove(T item)
        {
            if (!_items.Remove(item)) return false;

            NotifyChanged();
            return true;
        }

        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_items.Count)
            {
                ZLog.LogError($"ReactiveList<{typeof(T).Name}> 删除失败，下标越界：{index} (Count={_items.Count})");
                return;
            }

            _items.RemoveAt(index);
            NotifyChanged();
        }

        public bool GetValue(int index, out T value)
        {
            if ((uint)index >= (uint)_items.Count)
            {
                ZLog.LogError($"ReactiveList<{typeof(T).Name}> 读取失败，下标越界：{index} (Count={_items.Count})");
                value = default;
                return false;
            }

            value = _items[index];
            return true;
        }

        public bool SetValue(int index, T value)
        {
            if ((uint)index >= (uint)_items.Count)
            {
                ZLog.LogError($"ReactiveList<{typeof(T).Name}> 修改失败，下标越界：{index} (Count={_items.Count})");
                return false;
            }

            if (ValueComparer<T>.Compare(_items[index], value)) return false;

            _items[index] = value;
            NotifyChanged();
            return true;
        }

        public void Clear()
        {
            if (_items.Count == 0) return;

            _items.Clear();
            NotifyChanged();
        }

        public Action Subscribe(Action<IReadOnlyList<T>> callback)
        {
            if (callback == null)
            {
                ZLog.LogError($"ReactiveList<{typeof(T).Name}> 不能订阅空回调");
                return ReactiveSubscription.Empty;
            }

            try
            {
                callback(_items);
            }
            catch (Exception exception)
            {
                ZLog.LogError($"ReactiveList<{typeof(T).Name}> 首次回调执行异常，未建立订阅：{exception}");
                return ReactiveSubscription.Empty;
            }

            _callbacks.Subscribe(callback);
            return ReactiveSubscription.Create(() => _callbacks.Unsubscribe(callback));
        }

        public bool Unsubscribe(Action<IReadOnlyList<T>> callback)
        {
            return _callbacks.Unsubscribe(callback);
        }

        public void ClearSubscribers()
        {
            _callbacks.Clear();
        }

        private void NotifyChanged()
        {
            _callbacks.Invoke(_items);
        }
    }
}
