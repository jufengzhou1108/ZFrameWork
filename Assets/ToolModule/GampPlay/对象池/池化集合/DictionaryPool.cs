using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>
    /// Dictionary 对象池。继承 Pool，置空回调在首次使用前注册到 PoolCallbackFactory。
    /// 归还时由池负责清空，下一次 Get 得到的集合始终不包含上一次的数据。
    /// </summary>
    public class DictionaryPool<TKey, TValue> : Pool<Dictionary<TKey, TValue>>
    {
        private static DictionaryPool<TKey, TValue> instance;

        static DictionaryPool()
        {
            PoolCallbackFactory.Instance.Register<Dictionary<TKey, TValue>>(dictionary => dictionary.Clear());
        }

        /// <summary>该闭合类型唯一的 Dictionary 池。</summary>
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

        /// <summary>获取一个空的 Dictionary。</summary>
        public static new Dictionary<TKey, TValue> Get()
        {
            Pool<Dictionary<TKey, TValue>> pool = Instance;
            return pool.Get();
        }

        /// <summary>归还一个 Dictionary，池会清空后再收下。</summary>
        public static void Release(Dictionary<TKey, TValue> dictionary)
        {
            Instance.Add(dictionary);
        }
    }
}
