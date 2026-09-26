using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>
    /// LinkedList 对象池。归还时由池负责清空节点。
    /// </summary>
    public static class LinkedListPool<T>
    {
        private static readonly Pool<LinkedList<T>> _pool =
            new Pool<LinkedList<T>>(() => new LinkedList<T>(), destroyAction: null, resetAction: linkedList => linkedList.Clear());

        /// <summary>获取一个空的 LinkedList。</summary>
        public static LinkedList<T> Get()
        {
            return _pool.Get();
        }

        /// <summary>归还一个 LinkedList，池会清空节点后再收下。</summary>
        public static void Release(LinkedList<T> linkedList)
        {
            _pool.Add(linkedList);
        }
    }
}
