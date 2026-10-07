using System.Text;

namespace ZFrameWork
{
    /// <summary>
    /// StringBuilder 对象池。继承 Pool，置空回调在首次使用前注册到 PoolCallbackFactory。
    /// 归还时由池负责清空内容。
    /// </summary>
    public class StringBuilderPool : Pool<StringBuilder>
    {
        private static StringBuilderPool instance;

        static StringBuilderPool()
        {
            PoolCallbackFactory.Instance.Register<StringBuilder>(builder => builder.Clear());
        }

        /// <summary>全局唯一的 StringBuilder 池。</summary>
        public static StringBuilderPool Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new StringBuilderPool();
                }

                return instance;
            }
        }

        private StringBuilderPool()
            : base(() => new StringBuilder(), null)
        {
        }

        /// <summary>获取一个空的 StringBuilder。</summary>
        public static new StringBuilder Get()
        {
            Pool<StringBuilder> pool = Instance;
            return pool.Get();
        }

        /// <summary>归还一个 StringBuilder，池会清空内容后再收下。</summary>
        public static void Release(StringBuilder builder)
        {
            Instance.Add(builder);
        }
    }
}
