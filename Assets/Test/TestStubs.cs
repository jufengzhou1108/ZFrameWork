using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ZFrameWork.PlayTests
{
    /// <summary>
    /// 测试替身资源组：不依赖 Addressables，只服务动态注册的预制体，同步返回。
    /// </summary>
    public sealed class FakeResourceGroup : IResourceGroup
    {
        private readonly Dictionary<string, Object> _assets = new();
        private readonly List<string> _releasedKeys = new();

        /// <summary>已释放的资源 key 列表，供清理断言。</summary>
        public IReadOnlyList<string> ReleasedKeys => _releasedKeys;

        public void RegisterPrefab(string path, Object prefab)
        {
            _assets[path] = prefab;
        }

        public bool IsReleased(string key) => _releasedKeys.Contains(key);

        public T Load<T>(string path, string key) where T : Object
        {
            return _assets.TryGetValue(key, out Object asset) && asset is T typed ? typed : null;
        }

        public Task<T> LoadAsync<T>(string path, string key) where T : Object
        {
            return Task.FromResult(Load<T>(path, key));
        }

        public void Release<T>(string key) where T : Object
        {
            Release(key);
        }

        public void Release(string key)
        {
            if (!string.IsNullOrEmpty(key))
                _releasedKeys.Add(key);
        }

        public void ReleaseAll()
        {
            foreach (string key in _assets.Keys)
            {
                if (!_releasedKeys.Contains(key))
                    _releasedKeys.Add(key);
            }
        }
    }

    /// <summary>测试替身工厂：每次 Create 返回独立资源组，与真实工厂的多实例语义一致。</summary>
    public sealed class FakeFactory : IFactory
    {
        private readonly Dictionary<string, Object> _prefabs;

        public FakeFactory(Dictionary<string, Object> prefabs)
        {
            _prefabs = prefabs;
        }

        public IResourceGroup Create()
        {
            FakeResourceGroup group = new();
            foreach (KeyValuePair<string, Object> kv in _prefabs)
                group.RegisterPrefab(kv.Key, kv.Value);
            return group;
        }
    }

    /// <summary>单例界面桩。只记录是否走过 Close，用于验证清理路径确实走完整。</summary>
    public sealed class TestView : UIBase
    {
        public bool Closed;

        protected override void OnClose() => Closed = true;
    }

    /// <summary>弹窗桩：只需要能被 Push 进栈。</summary>
    public sealed class TestPopup : PopupBase<int> { }
}
