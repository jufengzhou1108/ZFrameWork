using System;
using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>
    /// 池回调工厂：记录"类型 → 默认化回调"，供对象池在构造时取用。
    ///
    /// 约定：
    /// - 全局单例，回调表在启动阶段一次给全（必须早于任何相关对象池的构造，否则池只会拿到空委托）；
    /// - 键是闭合类型：List&lt;int&gt; 与 List&lt;string&gt; 是两个不同的类，各注册各的；
    /// - 查不到该类型时返回按类型缓存的空委托，池侧因此可以无条件调用，不必判空；
    /// - 重复注册报错（不在运行期反复改表）。
    /// </summary>
    public sealed class PoolCallbackFactory : Singleton<PoolCallbackFactory>
    {
        /// <summary>按类型缓存空委托，未注册的类型每次查询都拿同一个实例，不产生额外分配。</summary>
        private static class EmptyCallback<T>
        {
            public static readonly Action<T> Instance = _ => { };
        }

        private readonly Dictionary<Type, Delegate> _callbacks = new();

        /// <summary>注册某类型的默认化回调。</summary>
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

        /// <summary>取某类型的默认化回调；没注册过则返回空委托。</summary>
        public Action<T> Get<T>()
        {
            if (_callbacks.TryGetValue(typeof(T), out Delegate callback))
                return (Action<T>)callback;

            return EmptyCallback<T>.Instance;
        }
    }
}
