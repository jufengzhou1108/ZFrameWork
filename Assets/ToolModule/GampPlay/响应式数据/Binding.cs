using System;
using System.Collections.Generic;

namespace ZFrameWork
{
    public sealed class Binding
    {
        private readonly List<Action> _unsubscribers = new();
        private bool _cleared;

        public void Bind<T>(ReactiveValue<T> source, Action<T> handler)
        {
            Add(source.Subscribe(handler));
        }

        public void Bind<T>(ReactiveList<T> source, Action<IReadOnlyList<T>> handler)
        {
            Add(source.Subscribe(handler));
        }

        public void Bind<TKey, TValue>(ReactiveDictionary<TKey, TValue> source,
            Action<IReadOnlyDictionary<TKey, TValue>> handler)
        {
            Add(source.Subscribe(handler));
        }

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
