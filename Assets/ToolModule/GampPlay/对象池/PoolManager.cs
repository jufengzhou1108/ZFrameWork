using System;
using System.Collections.Generic;

namespace ZFrameWork
{
    public class PoolManager
    {
        private readonly Dictionary<Type, IPool> _pools = new Dictionary<Type, IPool>();

        public void Register<T>(IPool<T> pool) where T : class
        {
            if (pool == null)
            {
                ZLog.LogError($"[PoolManager.Register] pool 参数为 null，类型 {typeof(T).Name} 注册被跳过。");
                return;
            }
            _pools[typeof(T)] = pool;
        }

        public IPool<T> Get<T>() where T : class
        {
            if (_pools.TryGetValue(typeof(T), out var pool))
                return pool as IPool<T>;
            return null;
        }

        public bool Remove<T>() where T : class
        {
            return _pools.Remove(typeof(T));
        }

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
