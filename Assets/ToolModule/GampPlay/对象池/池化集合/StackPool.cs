using System.Collections.Generic;

namespace ZFrameWork
{
    public class StackPool<T> : Pool<Stack<T>>
    {
        private static StackPool<T> instance;

        static StackPool()
        {
            PoolCallbackFactory.Instance.Register<Stack<T>>(stack => stack.Clear());
        }

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

        public static new Stack<T> Get()
        {
            Pool<Stack<T>> pool = Instance;
            return pool.Get();
        }

        public static void Release(Stack<T> stack)
        {
            Instance.Add(stack);
        }
    }
}
