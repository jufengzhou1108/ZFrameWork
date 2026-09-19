namespace ZFrameWork
{
    /// <summary>
    /// 默认资源方案：Addressables。
    /// 组内共享同一份 AddressablesLoadManager（引用计数全局一份），
    /// 每次 Create 返回独立资源组，各自的账本互不干扰。
    /// </summary>
    public sealed class AddressablesFactory : IFactory
    {
        private AddressablesLoadManager _manager;

        /// <summary>共享的资源加载管理器，首次使用时创建。</summary>
        public AddressablesLoadManager Manager => _manager ??= new AddressablesLoadManager();

        public IResourceGroup Create()
        {
            return new AddressablesGroup(Manager);
        }
    }
}
