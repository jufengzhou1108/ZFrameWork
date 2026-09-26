using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>
    /// List 对象池。归还时由池负责清空，下一次 Get 得到的集合始终不包含上一次的数据。
    /// </summary>
    public static class ListPool<T>
    {
        private static readonly Pool<List<T>> _pool =
            new Pool<List<T>>(() => new List<T>(), destroyAction: null, resetAction: list => list.Clear());

        /// <summary>池中当前可复用的集合数量。</summary>
        private static int Count => _pool.Count;

        /// <summary>池中最多保存的集合数量。</summary>
        private static int Capacity
        {
            get => _pool.Capacity;
            set => _pool.Capacity = value;
        }

        /// <summary>获取一个空的 List。</summary>
        public static List<T> Get()
        {
            return _pool.Get();
        }

        /// <summary>归还一个 List，池会清空后再收下。</summary>
        public static void Release(List<T> list)
        {
            _pool.Add(list);
        }

        /// <summary>清空池并销毁其中所有空闲集合。</summary>
        private static void Clear()
        {
            _pool.Clear();
        }
    }
}
