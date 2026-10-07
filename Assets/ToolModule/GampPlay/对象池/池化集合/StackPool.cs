using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>
    /// Stack 对象池。继承 Pool，置空回调在首次使用前注册到 PoolCallbackFactory。
    /// 归还时由池负责清空，下一次 Get 得到的集合始终不包含上一次的数据。
    /// </summary>
    public class StackPool<T> : Pool<Stack<T>>
    {
        private static StackPool<T> instance;

        static StackPool()
        {
            PoolCallbackFactory.Instance.Register<Stack<T>>(stack => stack.Clear());
        }

        /// <summary>该闭合类型唯一的 Stack 池。</summary>
        public static StackPool<T> Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new StackPool<T>();
                }

                return instance;
            }
        }

        private StackPool()
            : base(() => new Stack<T>(), null)
        {
        }

        /// <summary>获取一个空的 Stack。</summary>
        public static new Stack<T> Get()
        {
            Pool<Stack<T>> pool = Instance;
            return pool.Get();
        }

        /// <summary>归还一个 Stack，池会清空后再收下。</summary>
        public static void Release(Stack<T> stack)
        {
            Instance.Add(stack);
        }
    }
}
