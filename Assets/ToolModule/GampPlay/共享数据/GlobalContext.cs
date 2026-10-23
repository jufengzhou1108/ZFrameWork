using System;

namespace ZFrameWork
{
    // 全局系统上下文：对 SystemContext 的单例封装，整个进程共享一份。
    public sealed class GlobalContext : Singleton<GlobalContext>
    {
        private readonly SystemContext _context = new SystemContext();

        public void Set<T>(T model) where T : class
        {
            _context.Set(model);
        }

        public void Set(Type key, object model)
        {
            _context.Set(key, model);
        }

        public T Get<T>() where T : class
        {
            return _context.Get<T>();
        }

        public object Get(Type key)
        {
            return _context.Get(key);
        }

        public void Delete<T>() where T : class
        {
            _context.Delete<T>();
        }

        public void Delete(Type key)
        {
            _context.Delete(key);
        }
    }
}
