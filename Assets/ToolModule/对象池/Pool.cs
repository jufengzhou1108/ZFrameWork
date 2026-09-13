using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZFrameWork
{
    /// <summary>
    /// 对象池。通过构造函数注入创建和销毁的委托。
    /// </summary>
    public class Pool<T> : IPool<T> where T : class
    {
        private readonly Func<T> _createFunc;
        private readonly Action<T> _destroyAction;

        private readonly List<T> _freeObjects = new List<T>();
        private readonly List<float> _addTimes = new List<float>();
        private readonly HashSet<T> _tracked = new HashSet<T>();

        private int _capacity = int.MaxValue;
        private float _expireTime = -1f;
        private float _lastCleanupTime;

        /// <summary>当前池中空闲对象数量。</summary>
        public int Count => _freeObjects.Count;

        /// <summary>池的容量上限，超出部分归还时拒绝入池。</summary>
        public int Capacity
        {
            get => _capacity;
            set => _capacity = Math.Max(0, value);
        }

        /// <summary>过期时间（秒），<= 0 表示不启用过期清理。</summary>
        public float ExpireTime
        {
            get => _expireTime;
            set => _expireTime = value;
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="createFunc">创建对象的委托，不能为 null。</param>
        /// <param name="destroyAction">销毁对象的委托，可为 null。</param>
        public Pool(Func<T> createFunc, Action<T> destroyAction = null)
        {
            if (createFunc == null)
            {
                ZLog.LogError("[Pool.ctor] createFunc 参数为 null，池将无法创建新对象。");
                _createFunc = () => null;
            }
            else
            {
                _createFunc = createFunc;
            }
            _destroyAction = destroyAction;
        }

        /// <summary>
        /// 从池中获取对象。
        /// </summary>
        public T Get()
        {
            if (_expireTime > 0f)
            {
                float now = GetCurrentTime();
                if (now - _lastCleanupTime >= _expireTime)
                {
                    CleanExpired(now);
                    _lastCleanupTime = now;
                }
            }

            T obj;
            if (_freeObjects.Count > 0)
            {
                int last = _freeObjects.Count - 1;
                obj = _freeObjects[last];
                _freeObjects.RemoveAt(last);
                _addTimes.RemoveAt(last);
                _tracked.Remove(obj);
            }
            else
            {
                obj = _createFunc();
                if (obj == null)
                {
                    ZLog.LogError($"[Pool.Get] Create() 返回了 null，类型 {typeof(T).Name}。");
                    return null;
                }
            }

            CallOnGet(obj);
            return obj;
        }

        /// <summary>
        /// 将对象归还到池中。
        /// </summary>
        public void Add(T obj)
        {
            if (obj == null)
            {
                ZLog.LogError("[Pool.Add] obj 参数为 null，归还被跳过。");
                return;
            }

            if (_tracked.Contains(obj))
            {
                ZLog.LogError($"[Pool.Add] 类型 {typeof(T).Name} 的对象已被归还过，重复归还被跳过。");
                return;
            }

            if (_freeObjects.Count >= _capacity)
            {
                return;
            }

            CallOnAdd(obj);

            _freeObjects.Add(obj);
            _addTimes.Add(GetCurrentTime());
            _tracked.Add(obj);
        }

        /// <summary>清理所有过期对象。传入当前时间避免重复获取。</summary>
        public void CleanExpired(float now = -1f)
        {
            if (_expireTime <= 0f)
                return;

            if (now < 0f)
                now = GetCurrentTime();
            for (int i = _freeObjects.Count - 1; i >= 0; i--)
            {
                if (now - _addTimes[i] >= _expireTime)
                {
                    T obj = _freeObjects[i];
                    _freeObjects.RemoveAt(i);
                    _addTimes.RemoveAt(i);
                    _tracked.Remove(obj);
                    _destroyAction?.Invoke(obj);
                }
            }
        }

        /// <summary>清空池中所有空闲对象并销毁。</summary>
        public void Clear()
        {
            for (int i = _freeObjects.Count - 1; i >= 0; i--)
            {
                _tracked.Remove(_freeObjects[i]);
                _destroyAction?.Invoke(_freeObjects[i]);
            }
            _freeObjects.Clear();
            _addTimes.Clear();
        }

        /// <summary>获取当前时间（秒），子类可在 Unity 中重写为 Time.time。</summary>
        protected virtual float GetCurrentTime()
        {
            return Environment.TickCount / 1000f;
        }

        private void CallOnGet(T obj)
        {
            if (obj is IPoolable poolable)
                poolable.OnGet();
        }

        private void CallOnAdd(T obj)
        {
            if (obj is IPoolable poolable)
                poolable.OnAdd();
        }
    }
}
