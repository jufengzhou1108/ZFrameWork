namespace ZFrameWork
{
    /// <summary>
    /// 资源组工厂接口：一种资源方案对应一个实现。
    /// 实现类自行持有该方案所需的加载器与配置（如 Addressables 的加载管理器），
    /// 每次调用返回一个独立的资源组实例，其生命周期归调用方。
    /// </summary>
    public interface IFactory
    {
        /// <summary>创建一个新的资源组。</summary>
        IResourceGroup Create();
    }
}
