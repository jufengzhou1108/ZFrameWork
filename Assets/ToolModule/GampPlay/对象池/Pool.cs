using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZFrameWork
{
    public interface IPool
    {
        void Clear();
    }

    public interface IPool<T> : IPool where T : class
    {
        T Get();

        void Add(T obj);

        int Count { get; }

        int Capacity { get; }
    }

    public class Pool<T> : IPool<T> where T : class
    {
        private readonly Func<T> _createFunc;
        private readonly Action<T> _destroyAction;
        private readonly Action<T> _resetAction;

        private readonly List<T> _freeObjects = new List<T>();
        private readonly List<float> _addTimes = new List<float>();
        private readonly HashSet<T> _tracked = new HashSet<T>();

        private int _capacity = int.MaxValue;
        private float _expireTime = -1f;
        private float _lastCleanupTime;

        public int Count => _freeObjects.Count;

        public int Capacity
        {
            get => _capacity;
            set => _capacity = Math.Max(0, value);
        }

        public float ExpireTime
        {
            get => _expireTime;
            set => _expireTime = value;
        }

        public Pool(Func<T> createFunc, Action<T> destroyAction = null)
            : this(createFunc, destroyAction, null, false)
        {
        }

        public Pool(Func<T> createFunc, Action<T> destroyAction, Action<T> resetAction)
            : this(createFunc, destroyAction, resetAction, true)
        {
        }

        private Pool(Func<T> createFunc, Action<T> destroyAction, Action<T> resetAction, bool resetInjected)
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

            if (resetInjected && resetAction == null)
            {
                ZLog.LogError($"[Pool.ctor] 显式注入的 resetAction 为 null，类型 {typeof(T).Name} 改用工厂里的默认回调。");
                resetInjected = false;
            }

            _resetAction = resetInjected ? resetAction : PoolCallbackFactory.Instance.Get<T>();
        }

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

            return obj;
        }
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

            //归还即置为默认：规则由构造时定下的默认化回调统一负责
            _resetAction(obj);

            _freeObjects.Add(obj);
            _addTimes.Add(GetCurrentTime());
            _tracked.Add(obj);
        }

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

        protected virtual float GetCurrentTime()
        {
            return Environment.TickCount / 1000f;
        }
    }
}
