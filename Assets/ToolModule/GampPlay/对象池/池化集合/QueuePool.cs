using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>
    /// Queue 对象池。继承 Pool，置空回调在首次使用前注册到 PoolCallbackFactory。
    /// 归还时由池负责清空，下一次 Get 得到的集合始终不包含上一次的数据。
    /// </summary>
    public class QueuePool<T> : Pool<Queue<T>>
    {
        private static QueuePool<T> instance;

        static QueuePool()
        {
            PoolCallbackFactory.Instance.Register<Queue<T>>(queue => queue.Clear());
        }

        /// <summary>该闭合类型唯一的 Queue 池。</summary>
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

        /// <summary>获取一个空的 Queue。</summary>
        public static new Queue<T> Get()
        {
            Pool<Queue<T>> pool = Instance;
            return pool.Get();
        }

        /// <summary>归还一个 Queue，池会清空后再收下。</summary>
        public static void Release(Queue<T> queue)
        {
            Instance.Add(queue);
        }
    }
}
