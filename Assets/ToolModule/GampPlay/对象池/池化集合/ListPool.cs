using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>
    /// List 对象池。继承 Pool，置空回调在首次使用前注册到 PoolCallbackFactory。
    /// 归还时由池负责清空，下一次 Get 得到的集合始终不包含上一次的数据。
    /// </summary>
    public class ListPool<T> : Pool<List<T>>
    {
        private static ListPool<T> instance;

        static ListPool()
        {
            PoolCallbackFactory.Instance.Register<List<T>>(list => list.Clear());
        }

        /// <summary>该闭合类型唯一的 List 池。</summary>
        public static ListPool<T> Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ListPool<T>();
                }

                return instance;
            }
        }

        private ListPool()
            : base(() => new List<T>(), null)
        {
        }

        /// <summary>获取一个空的 List。</summary>
        public static new List<T> Get()
        {
            Pool<List<T>> pool = Instance;
            return pool.Get();
        }

        /// <summary>归还一个 List，池会清空后再收下。</summary>
        public static void Release(List<T> list)
        {
            Instance.Add(list);
        }
    }
}
