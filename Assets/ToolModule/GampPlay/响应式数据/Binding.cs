using System;
using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>
    /// 绑定收纳器：业务类通过 Bind 建立订阅，内部记录每个订阅返回的取消委托，
    /// 生命周期结束时调用 Clear 一句断开全部。Clear 幂等。
    /// </summary>
    public sealed class Binding
    {
        private readonly List<Action> _unsubscribers = new();
        private bool _cleared;

        /// <summary>订阅响应式值并登记取消委托。</summary>
        public void Bind<T>(ReactiveValue<T> source, Action<T> handler)
        {
            Add(source.Subscribe(handler));
        }

        /// <summary>订阅响应式列表并登记取消委托。</summary>
        public void Bind<T>(ReactiveList<T> source, Action<IReadOnlyList<T>> handler)
        {
            Add(source.Subscribe(handler));
        }

        /// <summary>订阅响应式字典并登记取消委托。</summary>
        public void Bind<TKey, TValue>(ReactiveDictionary<TKey, TValue> source,
            Action<IReadOnlyDictionary<TKey, TValue>> handler)
        {
            Add(source.Subscribe(handler));
        }

        /// <summary>断开全部订阅。幂等；Clear 后应丢弃本实例，不再复用。</summary>
        public void Clear()
        {
            if (_cleared)
                return;
            _cleared = true;

            foreach (Action unsubscribe in _unsubscribers)
                unsubscribe();
            _unsubscribers.Clear();
        }

        private void Add(Action unsubscribe)
        {
            if (_cleared)
            {
                ZLog.LogError("[Binding] Clear 之后不应继续 Bind，本条订阅已立即断开。");
                unsubscribe();
                return;
            }

            _unsubscribers.Add(unsubscribe);
        }
    }
}
