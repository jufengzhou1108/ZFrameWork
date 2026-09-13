using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>
    /// Dictionary 对象池。集合归还时会自动清空，下一次 Get 得到的集合始终不包含上一次的数据。
    /// </summary>
    public static class DictionaryPool<TKey, TValue>
    {
        private static readonly Pool<Dictionary<TKey, TValue>> _pool =
            new Pool<Dictionary<TKey, TValue>>(
                () => new Dictionary<TKey, TValue>(),
                dictionary => dictionary.Clear());

        /// <summary>池中当前可复用的集合数量。</summary>
        private static int Count => _pool.Count;

        /// <summary>池中最多保存的集合数量。</summary>
        private static int Capacity
        {
            get => _pool.Capacity;
            set => _pool.Capacity = value;
        }

        /// <summary>获取一个空的 Dictionary。</summary>
        public static Dictionary<TKey, TValue> Get()
        {
            return _pool.Get();
        }

        /// <summary>归还一个 Dictionary，归还时会自动清空。</summary>
        private static void Add(Dictionary<TKey, TValue> dictionary)
        {
            dictionary?.Clear();
            _pool.Add(dictionary);
        }

        /// <summary>归还一个 Dictionary，等同于 Add。</summary>
        public static void Release(Dictionary<TKey, TValue> dictionary)
        {
            Add(dictionary);
        }

        /// <summary>清空池并销毁其中所有空闲集合。</summary>
        private static void Clear()
        {
            _pool.Clear();
        }
    }
}
