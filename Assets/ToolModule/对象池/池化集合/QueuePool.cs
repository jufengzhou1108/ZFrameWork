using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>
    /// Queue 对象池。归还时由池负责清空，下一次 Get 得到的集合始终不包含上一次的数据。
    /// </summary>
    public static class QueuePool<T>
    {
        private static readonly Pool<Queue<T>> _pool =
            new Pool<Queue<T>>(() => new Queue<T>(), destroyAction: null, resetAction: queue => queue.Clear());

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

        /// <summary>归还一个 Queue，池会清空后再收下。</summary>
        public static void Release(Queue<T> queue)
        {
            _pool.Add(queue);
        }

        /// <summary>清空池并销毁其中所有空闲集合。</summary>
        private static void Clear()
        {
            _pool.Clear();
        }
    }
}
