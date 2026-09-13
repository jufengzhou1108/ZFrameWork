using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZFrameWork.PlayTests
{
    /// <summary>
    /// 测试替身资源组：不依赖 Addressables，持有动态注册的预制体，
    /// 可配置加载延迟（模拟乱序）与加载失败，并记录释放情况供清理断言。
    /// </summary>
    public sealed class FakeResourceGroup : IResourceGroup
    {
        /// <summary>全局失败开关：静态直读，保证先于用例创建的长寿组（如管理器组）也能被当前用例控制。</summary>
        public static bool FailAllLoadsConfig;

        /// <summary>全局按 key 延迟配置（毫秒），同样静态直读。</summary>
        public static readonly Dictionary<string, int> DelayConfig = new();

        private readonly Dictionary<string, Object> _assets = new();
        private readonly List<string> _releasedKeys = new();
        private bool _cooled;

        public bool IsCooled => _cooled;

        /// <summary>已释放的资源 key 列表，供验证清理。</summary>
        public IReadOnlyList<string> ReleasedKeys => _releasedKeys;

        public void RegisterPrefab(string path, Object prefab)
        {
            _assets[path] = prefab;
        }

        public T Load<T>(string path, string key) where T : Object
        {
            if (!EnsureActive())
                return null;
            if (FailAllLoadsConfig || !_assets.TryGetValue(key, out Object asset) || !(asset is T typed))
                return null;
            return typed;
        }

        public async Task<T> LoadAsync<T>(string path, string key) where T : Object
        {
            if (!EnsureActive())
                return null;
            if (DelayConfig.TryGetValue(key, out int keyDelay) && keyDelay > 0)
                await Task.Delay(keyDelay);
            return Load<T>(path, key);
        }

        public void Release<T>(string key) where T : Object
        {
            Release(key);
        }

        public void Release(string key)
        {
            if (!EnsureActive() || string.IsNullOrEmpty(key))
                return;
            _releasedKeys.Add(key);
        }

        public void ReleaseAll(bool coolDown = false)
        {
            if (IsCooled)
                return;
            foreach (string key in _assets.Keys)
            {
                if (!_releasedKeys.Contains(key))
                    _releasedKeys.Add(key);
            }
            if (coolDown)
                _cooled = true;
        }

        public void Dispose()
        {
            ReleaseAll(true);
            GC.SuppressFinalize(this);
        }

        public bool IsReleased(string key) => _releasedKeys.Contains(key);

        private bool EnsureActive()
        {
            return !IsCooled;
        }
    }
}
