using System.Collections.Generic;

namespace ZFrameWork
{
    public class ListPool<T> : Pool<List<T>>
    {
        private static ListPool<T> instance;

        static ListPool()
        {
            PoolCallbackFactory.Instance.Register<List<T>>(list => list.Clear());
        }

        public static ListPool<T> Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ListPool<T>();
                }

                return instance;
            }
        }

        private ListPool()
            : base(() => new List<T>(), null)
        {
        }

        public static new List<T> Get()
        {
            Pool<List<T>> pool = Instance;
            return pool.Get();
        }

        public static void Release(List<T> list)
        {
            Instance.Add(list);
        }
    }
}
