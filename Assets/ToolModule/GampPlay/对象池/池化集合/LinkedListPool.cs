using System.Collections.Generic;

namespace ZFrameWork
{
    public class LinkedListPool<T> : Pool<LinkedList<T>>
    {
        private static LinkedListPool<T> instance;

        static LinkedListPool()
        {
            PoolCallbackFactory.Instance.Register<LinkedList<T>>(linkedList => linkedList.Clear());
        }

        public static LinkedListPool<T> Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LinkedListPool<T>();
                }

                return instance;
            }
        }

        private LinkedListPool()
            : base(() => new LinkedList<T>(), null)
        {
        }

        public static new LinkedList<T> Get()
        {
            Pool<LinkedList<T>> pool = Instance;
            return pool.Get();
        }

        public static void Release(LinkedList<T> linkedList)
        {
            Instance.Add(linkedList);
        }
    }
}
