using System.Threading.Tasks;
using UnityEngine;

namespace ZFrameWork
{
    /// <summary>
    /// 业务侧使用的资源组抽象。具体资源方案可以提供各自的实现。
    /// 访问组只是业务对象持有的句柄，关闭时由持有者释放并置空，自身不维护生命周期状态。
    /// </summary>
    public interface IResourceGroup
    {
        T Load<T>(string path, string key) where T : UnityEngine.Object;

        Task<T> LoadAsync<T>(string path, string key) where T : UnityEngine.Object;

        void Release<T>(string key) where T : UnityEngine.Object;

        void ReleaseAll();
    }
}
