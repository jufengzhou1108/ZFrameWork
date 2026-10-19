namespace ZFrameWork
{
    public sealed class ShareModelCenter : Singleton<ShareModelCenter>
    {
        private readonly ShareContainer _container = new();

        public void Set<T>(string key, T model) where T : class
        {
            _container.Set(key, model);
        }

        public T Get<T>(string key) where T : class
        {
            return _container.Get<T>(key);
        }

        public void Delete(string key)
        {
            _container.Delete(key);
        }
    }
}
