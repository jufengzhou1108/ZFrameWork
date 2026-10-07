using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ZFrameWork
{
    /// <summary>
    /// 业务侧资源访问组。每个 key 在组内只持有一份引用。
    /// 显式调用 ReleaseAll 是正常路径；
    /// 忘记释放时由析构函数向 AddressablesHelper 登记孤儿账本，主线程代为释放。
    /// </summary>
    public sealed class AddressablesGroup : IResourceGroup
    {
        private readonly AddressablesLoadManager _manager;
        private HashSet<string> _managedKeys;

        public AddressablesGroup(AddressablesLoadManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _managedKeys = HashSetPool<string>.Get();
            AddressablesHelper.EnsureFlushHook();
        }

        /// <summary>业务忘记释放时兜底：向 Helper 登记账本快照，由主线程代为释放。</summary>
        ~AddressablesGroup()
        {
            // ReleaseAll 已归还集合并置空，这里即为空账，直接返回
            if (_managedKeys == null || _managedKeys.Count == 0)
                return;

            string[] keys = new string[_managedKeys.Count];
            _managedKeys.CopyTo(keys);
            AddressablesHelper.RegisterOrphan(_manager, keys);
        }

        /// <summary>同步加载资源，组内重复访问不会增加引用计数。</summary>
        public T Load<T>(string path, string key) where T : UnityEngine.Object
        {
            string managedKey = _manager.BuildKey<T>(key);
            if (string.IsNullOrEmpty(managedKey))
                return null;

            if (_managedKeys != null && _managedKeys.Contains(managedKey))
                return _manager.WaitForLoaded<T>(managedKey);

            T asset = _manager.Load<T>(path, key);
            if (asset == null)
                return null;

            (_managedKeys ??= HashSetPool<string>.Get()).Add(managedKey);
            return asset;
        }

        /// <summary>异步加载资源，组内重复访问只直接获取已加载资源。</summary>
        public async Task<T> LoadAsync<T>(string path, string key) where T : UnityEngine.Object
        {
            string managedKey = _manager.BuildKey<T>(key);
            if (string.IsNullOrEmpty(managedKey))
                return null;

            if (_managedKeys != null && _managedKeys.Contains(managedKey))
                return await _manager.WaitForLoadedAsync<T>(managedKey);

            // 预登记防并发重复计数；续体恢复时集合可能已被 ReleaseAll 归还进池，
            // 故捕获局部引用，并只在仍归本组所有时才回滚
            HashSet<string> managedKeys = _managedKeys ??= HashSetPool<string>.Get();
            managedKeys.Add(managedKey);
            T asset = await _manager.LoadAsync<T>(path, key);
            if (asset == null && ReferenceEquals(_managedKeys, managedKeys))
                managedKeys.Remove(managedKey);
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
            if (_managedKeys == null || string.IsNullOrEmpty(key))
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
        /// 释放组内全部资源，并归还池化集合。访问组本身保持可用，再次加载会重新取用集合。
        /// </summary>
        public void ReleaseAll()
        {
            if (_managedKeys == null)
                return;

            if (_managedKeys.Count > 0)
            {
                List<string> managedKeys = ListPool<string>.Get();
                foreach (string managedKey in _managedKeys)
                    managedKeys.Add(managedKey);
                _manager.ReleaseManagedKeys(managedKeys);
                ListPool<string>.Release(managedKeys);

                _managedKeys.Clear();
            }

            HashSetPool<string>.Release(_managedKeys);
            _managedKeys = null;
        }
    }
}
