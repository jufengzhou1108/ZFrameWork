using System;
using System.Collections.Generic;

namespace ZFrameWork
{
    public sealed class PoolCallbackFactory : Singleton<PoolCallbackFactory>
    {
        private static class EmptyCallback<T>
        {
            public static readonly Action<T> Instance = _ => { };
        }

        private readonly Dictionary<Type, Delegate> _callbacks = new();

        public void Register<T>(Action<T> resetCallback)
        {
            if (resetCallback == null)
            {
                ZLog.LogError($"[PoolCallbackFactory.Register] 回调为 null，类型 {typeof(T).Name} 注册被跳过。");
                return;
            }

            if (_callbacks.ContainsKey(typeof(T)))
            {
                ZLog.LogError($"[PoolCallbackFactory.Register] 类型 {typeof(T).Name} 已注册过默认化回调，重复注册被拒绝。");
                return;
            }

            _callbacks.Add(typeof(T), resetCallback);
        }

        public Action<T> Get<T>()
        {
            if (_callbacks.TryGetValue(typeof(T), out Delegate callback))
                return (Action<T>)callback;

            return EmptyCallback<T>.Instance;
        }
    }
}
