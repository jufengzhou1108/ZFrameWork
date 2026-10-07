using System;

namespace ZFrameWork
{
    /// <summary>在值发生变化时向订阅者传播当前值。</summary>
    public sealed class ReactiveValue<T>
    {
        private readonly ListAction<T> _callbacks = new();
        private T _value;

        public T Value
        {
            get => _value;
            set => SetValue(value);
        }

        public ReactiveValue()
        {
        }

        public ReactiveValue(T initialValue)
        {
            _value = initialValue;
        }

        public bool SetValue(T value)
        {
            if (ValueComparer<T>.Compare(_value, value)) return false;

            _value = value;
            _callbacks.Invoke(_value);
            return true;
        }

        public Action Subscribe(Action<T> callback)
        {
            if (callback == null)
            {
                ZLog.LogError($"ReactiveValue<{typeof(T).Name}> 不能订阅空回调");
                return ReactiveSubscription.Empty;
            }

            try
            {
                callback(_value);
            }
            catch (Exception exception)
            {
                ZLog.LogError($"ReactiveValue<{typeof(T).Name}> 首次回调执行异常，未建立订阅：{exception}");
                return ReactiveSubscription.Empty;
            }

            _callbacks.Subscribe(callback);
            return ReactiveSubscription.Create(() => _callbacks.Unsubscribe(callback));
        }

        public bool Unsubscribe(Action<T> callback)
        {
            return _callbacks.Unsubscribe(callback);
        }

        public void ClearSubscribers()
        {
            _callbacks.Clear();
        }
    }
}
