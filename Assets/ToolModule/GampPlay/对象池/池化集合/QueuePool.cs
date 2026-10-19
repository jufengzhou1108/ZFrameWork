using System.Collections.Generic;

namespace ZFrameWork
{
    public class QueuePool<T> : Pool<Queue<T>>
    {
        private static QueuePool<T> instance;

        static QueuePool()
        {
            PoolCallbackFactory.Instance.Register<Queue<T>>(queue => queue.Clear());
        }

        public static QueuePool<T> Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new QueuePool<T>();
                }

                return instance;
            }
        }

        private QueuePool()
            : base(() => new Queue<T>(), null)
        {
        }

        public static new Queue<T> Get()
        {
            Pool<Queue<T>> pool = Instance;
            return pool.Get();
        }

        public static void Release(Queue<T> queue)
        {
            Instance.Add(queue);
        }
    }
}
