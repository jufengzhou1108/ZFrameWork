using System;
using System.Threading.Tasks;
using UnityEngine;

namespace ZFrameWork
{
    /// <summary>
    /// 业务侧使用的资源组抽象。具体资源方案可以提供各自的实现。
    /// </summary>
    public interface IResourceGroup : IDisposable
    {
        bool IsCooled { get; }

        T Load<T>(string path, string key) where T : UnityEngine.Object;

        Task<T> LoadAsync<T>(string path, string key) where T : UnityEngine.Object;

        void Release<T>(string key) where T : UnityEngine.Object;

        void ReleaseAll(bool coolDown = false);
    }
}
