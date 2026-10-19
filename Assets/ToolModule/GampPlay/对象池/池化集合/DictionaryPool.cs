using System.Collections.Generic;

namespace ZFrameWork
{
    public class DictionaryPool<TKey, TValue> : Pool<Dictionary<TKey, TValue>>
    {
        private static DictionaryPool<TKey, TValue> instance;

        static DictionaryPool()
        {
            PoolCallbackFactory.Instance.Register<Dictionary<TKey, TValue>>(dictionary => dictionary.Clear());
        }

        public static DictionaryPool<TKey, TValue> Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DictionaryPool<TKey, TValue>();
                }

                return instance;
            }
        }

        private DictionaryPool()
            : base(() => new Dictionary<TKey, TValue>(), null)
        {
        }

        public static new Dictionary<TKey, TValue> Get()
        {
            Pool<Dictionary<TKey, TValue>> pool = Instance;
            return pool.Get();
        }

        public static void Release(Dictionary<TKey, TValue> dictionary)
        {
            Instance.Add(dictionary);
        }
    }
}
