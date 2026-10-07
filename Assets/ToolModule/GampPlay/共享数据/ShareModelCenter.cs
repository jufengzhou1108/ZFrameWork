namespace ZFrameWork
{
    /// <summary>
    /// 共享数据中心：对 ShareContainer 的单例封装，业务侧统一从这里分享与获取 Model。
    /// 非 Mono 单例；语义上只负责分享，不参与 Model 的生命周期——删除的责任在负责该 Model 生命周期的类。
    /// </summary>
    public sealed class ShareModelCenter : Singleton<ShareModelCenter>
    {
        private readonly ShareContainer _container = new();

        /// <summary>放入共享 Model。重复、传 null、空 key 都报错。</summary>
        public void Set<T>(string key, T model) where T : class
        {
            _container.Set(key, model);
        }

        /// <summary>获取共享 Model。无 key、空 key、类型不符都报错并返回 null。</summary>
        public T Get<T>(string key) where T : class
        {
            return _container.Get<T>(key);
        }

        /// <summary>取消分享。无 key、空 key 都报错。</summary>
        public void Delete(string key)
        {
            _container.Delete(key);
        }

        /// <summary>仅供测试：清空索引，不触碰任何已分享对象。</summary>
        internal void ClearForTest()
        {
            _container.ClearForTest();
        }
    }
}
