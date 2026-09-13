using System;
using UnityEngine;

namespace ZFrameWork
{
    /// <summary>
    /// 分层 UI 管理器非泛型基类：持有所在层的 Canvas 与资源组，
    /// 由 UIRoot 按 UIManagerConfig 的层级配置创建并 Bind。
    /// </summary>
    public abstract class UIManager
    {
        /// <summary>本层 Canvas，由 UIRoot 创建。</summary>
        protected Canvas Canvas { get; private set; }

        /// <summary>全局资源组工厂，初始化时由 UIRoot 下发。</summary>
        protected Func<IResourceGroup> ResourceGroupFactory { get; private set; }

        private IResourceGroup _resourceGroup;

        /// <summary>管理器自己的资源组，负责加载界面预制体，销毁时兜底释放。</summary>
        protected IResourceGroup ResourceGroup => _resourceGroup;

        internal void Bind(Canvas canvas, Func<IResourceGroup> factory)
        {
            Canvas = canvas;
            ResourceGroupFactory = factory;
            BindInstance();
            OnInitialize();
        }

        /// <summary>泛型子类回填静态 Instance。</summary>
        protected internal abstract void BindInstance();

        /// <summary>Bind 完成后的初始化钩子，此时 Canvas 与工厂已就绪。</summary>
        protected virtual void OnInitialize() { }

        /// <summary>场景切换清理入口，由 UIRoot.CleanupForSceneSwitch 调用。</summary>
        internal virtual void ClearAll() { }

        /// <summary>确保管理器资源组可用，失败时打日志并返回 false。</summary>
        protected bool EnsureResourceGroup()
        {
            if (!EnsureAlive())
                return false;

            if (_resourceGroup != null)
                return true;

            if (ResourceGroupFactory == null)
            {
                ZLog.LogError($"[{GetType().Name}] 缺少全局资源组工厂（UIRoot.SetResourceGroupFactory 未调用），无法创建管理器资源组。");
                return false;
            }

            _resourceGroup = ResourceGroupFactory();
            if (_resourceGroup == null)
            {
                ZLog.LogError($"[{GetType().Name}] 资源组工厂返回了空资源组。");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 验证管理器自身的关键依赖仍然存活（Unity 伪 null 检查）。
        /// Canvas 销毁意味着所在 UI 层已失效（如场景卸载后异步延续仍在执行），
        /// 此时管理器的一切操作都不再安全。所有公共操作入口都应先调用本方法。
        /// </summary>
        protected bool EnsureAlive()
        {
            if (Canvas == null)
            {
                ZLog.LogError($"[{GetType().Name}] 所在层 Canvas 已销毁，管理器已失效，拒绝操作。");
                return false;
            }
            return true;
        }

        /// <summary>释放管理器资源组持有的全部界面预制体引用。</summary>
        protected void ReleaseResourceGroup()
        {
            _resourceGroup?.ReleaseAll();
            _resourceGroup = null;
        }
    }

    /// <summary>
    /// 带静态访问入口的管理器基类。Instance 由 UIRoot 创建时赋值，
    /// 未赋值前的访问直接报错（调用方违反初始化顺序）。
    /// </summary>
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
