using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>
    /// LinkedList 对象池。继承 Pool，置空回调在首次使用前注册到 PoolCallbackFactory。
    /// 归还时由池负责清空节点。
    /// </summary>
    public class LinkedListPool<T> : Pool<LinkedList<T>>
    {
        private static LinkedListPool<T> instance;

        static LinkedListPool()
        {
            PoolCallbackFactory.Instance.Register<LinkedList<T>>(linkedList => linkedList.Clear());
        }

        /// <summary>该闭合类型唯一的 LinkedList 池。</summary>
        public static LinkedListPool<T> Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LinkedListPool<T>();
                }

                return instance;
            }
        }

        private LinkedListPool()
            : base(() => new LinkedList<T>(), null)
        {
        }

        /// <summary>获取一个空的 LinkedList。</summary>
        public static new LinkedList<T> Get()
        {
            Pool<LinkedList<T>> pool = Instance;
            return pool.Get();
        }

        /// <summary>归还一个 LinkedList，池会清空节点后再收下。</summary>
        public static void Release(LinkedList<T> linkedList)
        {
            Instance.Add(linkedList);
        }
    }
}
