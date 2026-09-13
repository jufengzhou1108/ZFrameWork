using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>
    /// LinkedList 对象池。归还时会自动清空节点。
    /// </summary>
    public static class LinkedListPool<T>
    {
        private static readonly Pool<LinkedList<T>> _pool =
            new Pool<LinkedList<T>>(() => new LinkedList<T>(), linkedList => linkedList.Clear());

        /// <summary>获取一个空的 LinkedList。</summary>
        public static LinkedList<T> Get()
        {
            return _pool.Get();
        }

        /// <summary>归还一个 LinkedList，归还前会自动清空节点。</summary>
        public static void Release(LinkedList<T> linkedList)
        {
            Add(linkedList);
        }

        private static void Add(LinkedList<T> linkedList)
        {
            linkedList?.Clear();
            _pool.Add(linkedList);
        }
    }
}
