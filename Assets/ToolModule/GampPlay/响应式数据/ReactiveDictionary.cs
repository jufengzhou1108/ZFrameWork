using System;
using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>在内容变化后向订阅者传播当前只读字典。</summary>
    public sealed class ReactiveDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _items;
        private readonly ListAction<IReadOnlyDictionary<TKey, TValue>> _callbacks = new();

        public int Count => _items.Count;
        public ReactiveDictionary()
        {
            _items = new Dictionary<TKey, TValue>();
        }

        public ReactiveDictionary(IDictionary<TKey, TValue> items)
        {
            _items = items == null
                ? new Dictionary<TKey, TValue>()
                : new Dictionary<TKey, TValue>(items);
        }

        public bool Add(TKey key, TValue value)
        {
            // 用 ContainsKey + Add 而非 Dictionary.TryAdd：后者是 .NET Standard 2.1+ API，Unity 的 netstandard2.0 档位不可用
            if (_items.ContainsKey(key))
            {
                ZLog.LogError($"ReactiveDictionary 添加失败，键已存在：{key}");
                return false;
            }

            _items.Add(key, value);
            NotifyChanged();
            return true;
        }

        public bool SetValue(TKey key, TValue value)
        {
            if (!_items.TryGetValue(key, out var oldValue))
            {
                ZLog.LogError($"ReactiveDictionary 修改失败，键不存在：{key}");
                return false;
            }

            if (ValueComparer<TValue>.Compare(oldValue, value)) return false;

            _items[key] = value;
            NotifyChanged();
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _items.TryGetValue(key, out value);
        }

        public bool ContainsKey(TKey key)
        {
            return _items.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            if (!_items.Remove(key)) return false;

            NotifyChanged();
            return true;
        }

        public void Clear()
        {
            if (_items.Count == 0) return;

            _items.Clear();
            NotifyChanged();
        }

        public Action Subscribe(Action<IReadOnlyDictionary<TKey, TValue>> callback)
        {
            if (callback == null)
            {
                ZLog.LogError($"ReactiveDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}> 不能订阅空回调");
                return ReactiveSubscription.Empty;
            }

            try
            {
                callback(_items);
            }
            catch (Exception exception)
            {
                ZLog.LogError($"ReactiveDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}> 首次回调执行异常，未建立订阅：{exception}");
                return ReactiveSubscription.Empty;
            }

            _callbacks.Subscribe(callback);
            return ReactiveSubscription.Create(() => _callbacks.Unsubscribe(callback));
        }

        public bool Unsubscribe(Action<IReadOnlyDictionary<TKey, TValue>> callback)
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
