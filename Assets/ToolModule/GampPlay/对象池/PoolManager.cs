using System;
using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>
    /// 池管理器，按类型统一管理所有池实例，提供查找和创建入口。
    /// </summary>
    public class PoolManager
    {
        private readonly Dictionary<Type, IPool> _pools = new Dictionary<Type, IPool>();

        /// <summary>注册一个池实例。</summary>
        public void Register<T>(IPool<T> pool) where T : class
        {
            if (pool == null)
            {
                ZLog.LogError($"[PoolManager.Register] pool 参数为 null，类型 {typeof(T).Name} 注册被跳过。");
                return;
            }
            _pools[typeof(T)] = pool;
        }

        /// <summary>获取某类型的池，未注册或类型不符则返回 null。</summary>
        public IPool<T> Get<T>() where T : class
        {
            if (_pools.TryGetValue(typeof(T), out var pool))
                return pool as IPool<T>;
            return null;
        }

        /// <summary>移除某类型的池。</summary>
        public bool Remove<T>() where T : class
        {
            return _pools.Remove(typeof(T));
        }

        /// <summary>清空所有池，释放所有空闲对象。</summary>
        public void Clear()
        {
            foreach (var kvp in _pools)
            {
                kvp.Value.Clear();
            }
            _pools.Clear();
        }
    }
}
