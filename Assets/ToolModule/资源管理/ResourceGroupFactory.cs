using System;

namespace ZFrameWork
{
    /// <summary>
    /// 全局资源组工厂。业务与管理器统一从这里获取资源组，
    /// 内部共享同一份 AddressablesLoadManager（引用计数全局一份）。
    /// 默认创建 AddressablesGroup；资源方案变更时通过 SetCreator 整体替换。
    /// </summary>
    public static class ResourceGroupFactory
    {
        private static AddressablesLoadManager _manager;
        private static Func<IResourceGroup> _creator;

        /// <summary>共享的资源加载管理器，首次访问时创建，全进程一份。</summary>
        public static AddressablesLoadManager Manager => _manager ??= new AddressablesLoadManager();

        /// <summary>创建一个新的资源组。每次调用返回独立实例，生命周期归调用方。</summary>
        public static IResourceGroup Create()
        {
            Func<IResourceGroup> creator = _creator;
            if (creator != null)
                return creator();

            return new AddressablesGroup(Manager);
        }

        /// <summary>
        /// 替换资源组的创建方式（整体切换资源方案时使用）。
        /// 传 null 恢复默认的 AddressablesGroup。
        /// </summary>
        public static void SetCreator(Func<IResourceGroup> creator)
        {
            _creator = creator;
        }
    }
}
