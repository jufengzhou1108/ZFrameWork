using System.Threading.Tasks;
using UnityEngine;

namespace ZFrameWork
{
    public interface IResourceGroup
    {
        T Load<T>(string path, string key) where T : UnityEngine.Object;

        Task<T> LoadAsync<T>(string path, string key) where T : UnityEngine.Object;

        void Release<T>(string key) where T : UnityEngine.Object;

        void ReleaseAll();
    }
}
