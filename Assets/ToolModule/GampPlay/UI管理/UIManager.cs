using System;
using UnityEngine;

namespace ZFrameWork
{
    public abstract class UIManager
    {
        protected Canvas Canvas { get; private set; }

        private IResourceGroup _resourceGroup;

        protected IResourceGroup ResourceGroup => _resourceGroup;

        internal void Bind(Canvas canvas)
        {
            Canvas = canvas;
            BindInstance();
            OnInitialize();
        }

        protected internal abstract void BindInstance();

        protected virtual void OnInitialize() { }

        internal virtual void ClearAll() { }

        protected bool EnsureResourceGroup()
        {
            if (!EnsureAlive())
                return false;

            if (_resourceGroup != null)
                return true;

            _resourceGroup = ResourceGroupFactory.Create();
            if (_resourceGroup == null)
            {
                ZLog.LogError($"[{GetType().Name}] 资源组工厂返回了空资源组。");
                return false;
            }
            return true;
        }

        protected bool EnsureAlive()
        {
            if (Canvas == null)
            {
                ZLog.LogError($"[{GetType().Name}] 所在层 Canvas 已销毁，管理器已失效，拒绝操作。");
                return false;
            }
            return true;
        }

        protected void ReleaseResourceGroup()
        {
            _resourceGroup?.ReleaseAll();
            _resourceGroup = null;
        }
    }

    public abstract class UIManager<T> : UIManager where T : UIManager<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                    ZLog.LogError($"[UIManager] {typeof(T).Name} 尚未由 UIRoot 创建，禁止在 UIRoot 初始化前访问。");
                return _instance;
            }
        }

        protected internal override void BindInstance() => _instance = (T)this;
    }
}
