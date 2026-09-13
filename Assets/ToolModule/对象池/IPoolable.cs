namespace ZFrameWork
{
    /// <summary>
    /// 池对象回调接口（可选实现）。让对象感知入池和出池。
    /// </summary>
    public interface IPoolable
    {
        /// <summary>从池中被取出时调用。</summary>
        void OnGet();

        /// <summary>归还到池中时调用。</summary>
        void OnAdd();
    }
}
