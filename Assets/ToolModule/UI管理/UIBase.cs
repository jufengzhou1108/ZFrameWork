using System;
using System.Threading.Tasks;
using UnityEngine;

namespace ZFrameWork
{
    /// <summary>
    /// UI 界面生命周期基类。四种操作 Open/Show/Hide/Close 由管理器驱动，
    /// 子类重写 On 系列方法扩展行为；On 函数在 Open/Show 之后、Hide/Close 之前调用。
    /// 资源通过资源组加载，界面关闭时统一释放。
    /// </summary>
    public abstract class UIBase : MonoBehaviour
    {
        private enum LifecycleState
        {
            Closed,
            Opened,
            Shown,
            Hidden
        }

        private LifecycleState _state = LifecycleState.Closed;
        private IResourceGroup _resourceGroup;
        private string _key;

        public IResourceGroup ResourceGroup => _resourceGroup;
        public bool IsOpen => _state != LifecycleState.Closed;
        public bool IsShown => _state == LifecycleState.Shown;
        public string Key => _key;

        /// <summary>
        /// 设置 Controller 的基类函数。ViewModel 与 Controller 是业务自己的普通类，基类不持有它们；
        /// 子类选择性重写本函数，做自己的类型化登记。
        /// 约定：View 在 OnOpen 中创建 ViewModel 与 Controller，完成 View->ViewModel 绑定后
        /// 将 ViewModel 注入 Controller；界面关闭时的解绑与回收由子类在 OnClose 中自行处理。
        /// </summary>
        protected virtual void SetController() { }

        /// <summary>子类重写此方法创建自己的资源组，返回空则使用管理器下发的工厂。</summary>
        protected virtual IResourceGroup InitializeResourceGroup() => null;

        protected virtual void OnOpen()
        {
            // 约定：需要 ViewModel/Controller 的界面在此创建，完成 View->ViewModel 绑定后注入，
            // 并重写 SetController 做类型化登记；不需要的界面（直接读 Model）什么都不用做。
        }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnClose() { }

        protected T LoadResource<T>(string path, string key) where T : UnityEngine.Object
        {
            if (!EnsureLoadable("加载资源"))
                return null;
            return _resourceGroup.Load<T>(path, key);
        }

        protected Task<T> LoadResourceAsync<T>(string path, string key) where T : UnityEngine.Object
        {
            if (!EnsureLoadable("异步加载资源"))
                return Task.FromResult<T>(null);
            return _resourceGroup.LoadAsync<T>(path, key);
        }

        protected void ReleaseResource<T>(string key) where T : UnityEngine.Object
        {
            _resourceGroup?.Release<T>(key);
        }

        private bool EnsureLoadable(string operation)
        {
            if (_resourceGroup == null)
            {
                ZLog.LogError($"[{GetType().Name}] 资源组尚未初始化，无法{operation}。");
                return false;
            }
            return true;
        }

        internal bool EnsureResourceGroup()
        {
            if (_resourceGroup != null)
                return true;

            _resourceGroup = InitializeResourceGroup();
            if (_resourceGroup == null)
                _resourceGroup = ResourceGroupFactory.Create();
            if (_resourceGroup == null)
            {
                ZLog.LogError($"[{GetType().Name}] 缺少资源组，UI 面板无法管理自身资源。");
                return false;
            }
            return true;
        }

        internal void SetKeyInternal(string key)
        {
            _key = key;
        }

        internal void OpenInternal()
        {
            if (IsOpen)
                return;

            _state = LifecycleState.Opened;
            OnOpen();
        }

        internal void ShowInternal()
        {
            if (_state == LifecycleState.Shown)
                return;
            if (_state == LifecycleState.Closed)
                OpenInternal();

            _state = LifecycleState.Shown;
            gameObject.SetActive(true);
            OnShow();
        }

        internal void HideInternal()
        {
            if (_state != LifecycleState.Shown)
                return;

            OnHide();
            gameObject.SetActive(false);
            _state = LifecycleState.Hidden;
        }

        internal void CloseInternal()
        {
            if (_state == LifecycleState.Closed)
                return;
            if (_state == LifecycleState.Shown)
                HideInternal();

            OnClose();
            _resourceGroup?.ReleaseAll();
            _resourceGroup = null;
            _state = LifecycleState.Closed;
        }
    }
}
