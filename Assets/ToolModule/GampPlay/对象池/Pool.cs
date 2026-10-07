using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZFrameWork
{
    /// <summary>
    /// 对象池的非泛型接口。管理器要按类型统一清理各种池，
    /// 而泛型接口无法在不带类型参数的情况下调用，故清理入口放在这里。
    /// </summary>
    public interface IPool
    {
        /// <summary>清空池，释放所有空闲对象。</summary>
        void Clear();
    }

    /// <summary>
    /// 对象池对外接口，定义借出和归还行为。
    /// </summary>
    public interface IPool<T> : IPool where T : class
    {
        /// <summary>
        /// 从池中获取一个对象。池中有空闲则直接返回，池空则创建新对象。
        /// </summary>
        T Get();

        /// <summary>
        /// 将对象归还到池中。
        /// </summary>
        void Add(T obj);

        /// <summary>当前池中空闲对象数量。</summary>
        int Count { get; }

        /// <summary>池的容量上限。</summary>
        int Capacity { get; }
    }

    /// <summary>
    /// 对象池。通过构造函数注入创建、销毁与默认化的委托。
    /// 默认化回调决定"对象归还后如何回到默认状态"：
    /// 构造时显式注入优先，未注入（或注入 null，会报错）则从 PoolCallbackFactory 取该类型的默认回调，
    /// 工厂取不到会返回空委托，因此池内部可以无条件调用，不必判空。
    /// </summary>
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
        /// 创建对象池。默认化回调从 PoolCallbackFactory 按类型取。
        /// </summary>
        /// <param name="createFunc">创建对象的委托，不能为 null。</param>
        /// <param name="destroyAction">销毁对象的委托，可为 null。</param>
        public Pool(Func<T> createFunc, Action<T> destroyAction = null)
            : this(createFunc, destroyAction, null, false)
        {
        }

        /// <summary>
        /// 创建对象池并显式注入默认化回调（优先于工厂里的默认回调）。
        /// </summary>
        /// <param name="createFunc">创建对象的委托，不能为 null。</param>
        /// <param name="destroyAction">销毁对象的委托，可为 null。</param>
        /// <param name="resetAction">默认化回调，传 null 会报错并回落到工厂。</param>
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

            //归还即置为默认：规则由构造时定下的默认化回调统一负责
            _resetAction(obj);

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
    }
}
