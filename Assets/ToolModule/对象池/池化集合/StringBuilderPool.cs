using System.Text;

namespace ZFrameWork
{
    /// <summary>
    /// StringBuilder 对象池。归还时由池负责清空内容。
    /// </summary>
    public static class StringBuilderPool
    {
        private static readonly Pool<StringBuilder> _pool =
            new Pool<StringBuilder>(() => new StringBuilder(), destroyAction: null, resetAction: builder => builder.Clear());

        /// <summary>获取一个空的 StringBuilder。</summary>
        public static StringBuilder Get()
        {
            return _pool.Get();
        }

        /// <summary>归还一个 StringBuilder，池会清空内容后再收下。</summary>
        public static void Release(StringBuilder builder)
        {
            _pool.Add(builder);
        }
    }
}
