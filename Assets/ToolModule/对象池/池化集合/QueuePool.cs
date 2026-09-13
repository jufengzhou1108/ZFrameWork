using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>
    /// Queue 对象池。集合归还时会自动清空，下一次 Get 得到的集合始终不包含上一次的数据。
    /// </summary>
    public static class QueuePool<T>
    {
        private static readonly Pool<Queue<T>> _pool =
            new Pool<Queue<T>>(() => new Queue<T>(), queue => queue.Clear());

        /// <summary>池中当前可复用的集合数量。</summary>
        private static int Count => _pool.Count;

        /// <summary>池中最多保存的集合数量。</summary>
        private static int Capacity
        {
            get => _pool.Capacity;
            set => _pool.Capacity = value;
        }

        /// <summary>获取一个空的 Queue。</summary>
        public static Queue<T> Get()
        {
            return _pool.Get();
        }

        /// <summary>归还一个 Queue，归还时会自动清空。</summary>
        private static void Add(Queue<T> queue)
        {
            queue?.Clear();
            _pool.Add(queue);
        }

        /// <summary>归还一个 Queue，等同于 Add。</summary>
        public static void Release(Queue<T> queue)
        {
            Add(queue);
        }

        /// <summary>清空池并销毁其中所有空闲集合。</summary>
        private static void Clear()
        {
            _pool.Clear();
        }
    }
}
