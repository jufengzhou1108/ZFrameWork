using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ZFrameWork
{
    /// <summary>
    /// 池管理器，按类型统一管理所有池实例，提供查找和创建入口。
    /// </summary>
    public class PoolManager
    {
        private readonly Dictionary<Type, object> _pools = new Dictionary<Type, object>();

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

        /// <summary>获取某类型的池，未注册则返回 null。</summary>
        public IPool<T> Get<T>() where T : class
        {
            if (_pools.TryGetValue(typeof(T), out var pool))
                return (IPool<T>)pool;
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
                // 通过反射调用池的 Clear 方法
                var poolType = kvp.Value.GetType();
                var clearMethod = poolType.GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance);
                clearMethod?.Invoke(kvp.Value, null);
            }
            _pools.Clear();
        }
    }
}
