using System.Text;

namespace ZFrameWork
{
    public class StringBuilderPool : Pool<StringBuilder>
    {
        private static StringBuilderPool instance;

        static StringBuilderPool()
        {
            PoolCallbackFactory.Instance.Register<StringBuilder>(builder => builder.Clear());
        }

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

        public static new StringBuilder Get()
        {
            Pool<StringBuilder> pool = Instance;
            return pool.Get();
        }

        public static void Release(StringBuilder builder)
        {
            Instance.Add(builder);
        }
    }
}
