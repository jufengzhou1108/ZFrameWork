using System.Text;

namespace ZFrameWork
{
    /// <summary>
    /// StringBuilder 对象池。归还时会自动清空内容。
    /// </summary>
    public static class StringBuilderPool
    {
        private static readonly Pool<StringBuilder> _pool =
            new Pool<StringBuilder>(() => new StringBuilder(), builder => builder.Clear());

        /// <summary>获取一个空的 StringBuilder。</summary>
        public static StringBuilder Get()
        {
            return _pool.Get();
        }

        /// <summary>归还一个 StringBuilder，归还前会自动清空内容。</summary>
        public static void Release(StringBuilder builder)
        {
            Add(builder);
        }

        private static void Add(StringBuilder builder)
        {
            builder?.Clear();
            _pool.Add(builder);
        }
    }
}
