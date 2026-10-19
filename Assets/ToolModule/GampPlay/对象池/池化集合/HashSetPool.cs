using System.Collections.Generic;

namespace ZFrameWork
{
    public class HashSetPool<T> : Pool<HashSet<T>>
    {
        private static HashSetPool<T> instance;

        static HashSetPool()
        {
            PoolCallbackFactory.Instance.Register<HashSet<T>>(hashSet => hashSet.Clear());
        }

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

        public static new HashSet<T> Get()
        {
            Pool<HashSet<T>> pool = Instance;
            return pool.Get();
        }

        public static void Release(HashSet<T> hashSet)
        {
            Instance.Add(hashSet);
        }
    }
}
