using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>
    /// Stack 对象池。归还时由池负责清空，下一次 Get 得到的集合始终不包含上一次的数据。
    /// </summary>
    public static class StackPool<T>
    {
        private static readonly Pool<Stack<T>> _pool =
            new Pool<Stack<T>>(() => new Stack<T>(), destroyAction: null, resetAction: stack => stack.Clear());

        /// <summary>池中当前可复用的集合数量。</summary>
        private static int Count => _pool.Count;

        /// <summary>池中最多保存的集合数量。</summary>
        private static int Capacity
        {
            get => _pool.Capacity;
            set => _pool.Capacity = value;
        }

        /// <summary>获取一个空的 Stack。</summary>
        public static Stack<T> Get()
        {
            return _pool.Get();
        }

        /// <summary>归还一个 Stack，池会清空后再收下。</summary>
        public static void Release(Stack<T> stack)
        {
            _pool.Add(stack);
        }

        /// <summary>清空池并销毁其中所有空闲集合。</summary>
        private static void Clear()
        {
            _pool.Clear();
        }
    }
}
