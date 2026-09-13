using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ZFrameWork
{
    /// <summary>
    /// 业务侧资源访问组。每个 key 在组内只持有一份引用。
    /// 显式调用 ReleaseAll/Dispose 是正常路径；
    /// 忘记释放时由析构函数向 AddressablesHelper 登记孤儿账本，主线程代为释放。
    /// </summary>
    public sealed class AddressablesGroup : IResourceGroup
    {
        private readonly AddressablesLoadManager _manager;
        private HashSet<string> _managedKeys;
        private bool _cooled;

        public AddressablesGroup(AddressablesLoadManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _managedKeys = HashSetPool<string>.Get();
            AddressablesHelper.EnsureFlushHook();
        }

        /// <summary>业务忘记释放时兜底：向 Helper 登记账本快照，由主线程代为释放。</summary>
        ~AddressablesGroup()
        {
            // 冷却即已还清账；正常路径 Dispose 会 SuppressFinalize，不会进到这里
            if (_cooled || _managedKeys == null || _managedKeys.Count == 0)
                return;

            string[] keys = new string[_managedKeys.Count];
            _managedKeys.CopyTo(keys);
            AddressablesHelper.RegisterOrphan(_manager, keys);
        }

        /// <summary>访问组是否已经冷却，冷却后不再提供加载和释放功能。</summary>
        public bool IsCooled => _cooled;

        /// <summary>同步加载资源，组内重复访问不会增加引用计数。</summary>
        public T Load<T>(string path, string key) where T : UnityEngine.Object
        {
            if (!EnsureActive())
                return null;

            string managedKey = _manager.BuildKey<T>(key);
            if (string.IsNullOrEmpty(managedKey))
                return null;

            if (_managedKeys.Contains(managedKey))
                return _manager.WaitForLoaded<T>(managedKey);

            T asset = _manager.Load<T>(path, key);
            if (asset == null)
                return null;

            _managedKeys.Add(managedKey);
            return asset;
        }

        /// <summary>异步加载资源，组内重复访问只直接获取已加载资源。</summary>
        public async Task<T> LoadAsync<T>(string path, string key) where T : UnityEngine.Object
        {
            if (!EnsureActive())
                return null;

            string managedKey = _manager.BuildKey<T>(key);
            if (string.IsNullOrEmpty(managedKey))
                return null;

            if (_managedKeys.Contains(managedKey))
                return await _manager.WaitForLoadedAsync<T>(managedKey);

            _managedKeys.Add(managedKey);
            T asset = await _manager.LoadAsync<T>(path, key);
            if (asset == null)
                _managedKeys?.Remove(managedKey);
            return asset;
        }

        /// <summary>释放组内指定类型和 key 的资源引用。</summary>
        public void Release<T>(string key) where T : UnityEngine.Object
        {
            Release(key);
        }

        /// <summary>按 key 释放单个资源，适用于调用方不保留资源类型的场景。</summary>
        public void Release(string key)
        {
            if (!EnsureActive() || string.IsNullOrEmpty(key))
                return;

            string prefix = key + "_";
            List<string> matchingKeys = ListPool<string>.Get();
            foreach (string managedKey in _managedKeys)
            {
                if (managedKey.StartsWith(prefix, StringComparison.Ordinal))
                    matchingKeys.Add(managedKey);
            }

            for (int i = 0; i < matchingKeys.Count; i++)
            {
                if (_managedKeys.Remove(matchingKeys[i]))
                    _manager.ReleaseManagedKey(matchingKeys[i]);
            }
            ListPool<string>.Release(matchingKeys);
        }

        /// <summary>
        /// 释放组内全部资源。coolDown 为 true 时，访问组进入冷却状态且不再提供功能。
        /// </summary>
        public void ReleaseAll(bool coolDown = false)
        {
            if (_cooled)
                return;

            if (_managedKeys != null && _managedKeys.Count > 0)
            {
                List<string> managedKeys = ListPool<string>.Get();
                foreach (string managedKey in _managedKeys)
                    managedKeys.Add(managedKey);
                _manager.ReleaseManagedKeys(managedKeys);
                ListPool<string>.Release(managedKeys);

                _managedKeys.Clear();
            }

            if (coolDown)
            {
                _cooled = true;
                HashSetPool<string>.Release(_managedKeys);
                _managedKeys = null;
            }
        }

        /// <summary>确定性释放并冷却访问组。</summary>
        public void Dispose()
        {
            ReleaseAll(true);
            GC.SuppressFinalize(this);
        }

        private bool EnsureActive()
        {
            if (_cooled)
            {
                ZLog.LogError("[AddressablesGroup] 访问组已经冷却，不能继续使用。");
                return false;
            }

            return true;
        }
    }
}
