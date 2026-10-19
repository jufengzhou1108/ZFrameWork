using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ZFrameWork
{
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

        ~AddressablesGroup()
        {
            // ReleaseAll 已归还集合并置空，这里即为空账，直接返回
            if (_managedKeys == null || _managedKeys.Count == 0)
                return;

            string[] keys = new string[_managedKeys.Count];
            _managedKeys.CopyTo(keys);
            AddressablesHelper.RegisterOrphan(_manager, keys);
        }

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

        public void Release<T>(string key) where T : UnityEngine.Object
        {
            Release(key);
        }

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
