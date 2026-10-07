using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>
    /// HashSet 对象池。继承 Pool，置空回调在首次使用前注册到 PoolCallbackFactory。
    /// 归还时由池负责清空，下一次 Get 得到的集合始终不包含上一次的数据。
    /// </summary>
    public class HashSetPool<T> : Pool<HashSet<T>>
    {
        private static HashSetPool<T> instance;

        static HashSetPool()
        {
            PoolCallbackFactory.Instance.Register<HashSet<T>>(hashSet => hashSet.Clear());
        }

        /// <summary>该闭合类型唯一的 HashSet 池。</summary>
        public static HashSetPool<T> Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new HashSetPool<T>();
                }

                return instance;
            }
        }

        private HashSetPool()
            : base(() => new HashSet<T>(), null)
        {
        }

        /// <summary>获取一个空的 HashSet。</summary>
        public static new HashSet<T> Get()
        {
            Pool<HashSet<T>> pool = Instance;
            return pool.Get();
        }

        /// <summary>归还一个 HashSet，池会清空后再收下。</summary>
        public static void Release(HashSet<T> hashSet)
        {
            Instance.Add(hashSet);
        }
    }
}
