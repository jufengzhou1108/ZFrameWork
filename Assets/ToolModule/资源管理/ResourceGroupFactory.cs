namespace ZFrameWork
{
    /// <summary>
    /// 全局资源组工厂。业务与管理器统一从这里获取资源组。
    /// 内部持有一个 IFactory 实现；_factory 的赋值只发生在 SetFactory 一处，
    /// 未初始化时由 EnsureDefault 经 SetFactory 落默认方案（AddressablesFactory）。
    /// 更换资源方案时只需 SetFactory 替换工厂实例，调用方无感。
    /// </summary>
    public static class ResourceGroupFactory
    {
        private static IFactory _factory;

        /// <summary>当前使用的资源方案工厂；未初始化时先落默认方案再返回。</summary>
        public static IFactory Factory
        {
            get
            {
                EnsureDefault();
                return _factory;
            }
        }

        /// <summary>创建一个新的资源组。每次调用返回独立实例，生命周期归调用方。</summary>
        public static IResourceGroup Create()
        {
            EnsureDefault();
            return _factory.Create();
        }

        /// <summary>
        /// 替换资源方案工厂（整体切换资源方案或测试注入时使用），_factory 的唯一赋值点。
        /// 传 null 恢复默认的 AddressablesFactory。
        /// </summary>
        public static void SetFactory(IFactory factory)
        {
            _factory = factory ?? new AddressablesFactory();
        }

        private static void EnsureDefault()
        {
            if (_factory == null)
                SetFactory(new AddressablesFactory());
        }
    }
}
