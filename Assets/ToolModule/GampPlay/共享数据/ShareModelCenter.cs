using System;

namespace ZFrameWork
{
    public sealed class ShareModelCenter : Singleton<ShareModelCenter>
    {
        private readonly ShareContainer _container = new();

        public void Set<T>(T model) where T : class
        {
            _container.Set(model);
        }

        public void Set(Type key, object model)
        {
            _container.Set(key, model);
        }

        public T Get<T>() where T : class
        {
            return _container.Get<T>();
        }

        public object Get(Type key)
        {
            return _container.Get(key);
        }

        public void Delete<T>() where T : class
        {
            _container.Delete<T>();
        }

        public void Delete(Type key)
        {
            _container.Delete(key);
        }
    }
}
