using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ZFrameWork
{
    /// <summary>
    /// Addressables 资源加载管理器。
    /// Addressables 只负责一次实际加载和最终释放，业务引用计数由本管理器维护。
    /// </summary>
    public sealed class AddressablesLoadManager
    {
        private readonly Dictionary<string, ResourceEntry> _entries =
            new Dictionary<string, ResourceEntry>();

        /// <summary>
        /// 同步加载资源。path 用于 Addressables 查找，key 用于资源后续定位。
        /// </summary>
        public T Load<T>(string path, string key) where T : UnityEngine.Object
        {
            if (!ValidateArguments<T>(path, key, out string managedKey))
                return null;

            if (_entries.TryGetValue(managedKey, out ResourceEntry existing))
            {
                existing.ReferenceCount++;
                if (existing.IsLoading)
                {
                    existing.Handle.WaitForCompletion();
                    CompleteLoad(existing, existing.Handle.Result as UnityEngine.Object);
                }
                return existing.Asset as T;
            }

            ResourceEntry entry = CreateEntry(path, managedKey);
            _entries.Add(managedKey, entry);

            try
            {
                AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(path);
                entry.Handle = handle;
                handle.WaitForCompletion();
                CompleteLoad(entry, handle.Result);
                return entry.Asset as T;
            }
            catch (Exception exception)
            {
                FailEntry(entry, exception);
                return null;
            }
        }

        /// <summary>
        /// 异步加载资源。加载失败、路径不存在或加载期间资源被释放时回调参数为 null。
        /// </summary>
        public async Task<T> LoadAsync<T>(string path, string key) where T : UnityEngine.Object
        {
            if (!ValidateArguments<T>(path, key, out string managedKey))
                return null;

            if (_entries.TryGetValue(managedKey, out ResourceEntry existing))
            {
                existing.ReferenceCount++;
                if (existing.IsLoading)
                {
                    if (existing.LoadTask != null)
                        await existing.LoadTask;
                    else
                    {
                        existing.Handle.WaitForCompletion();
                        CompleteLoad(existing, existing.Handle.Result as UnityEngine.Object);
                    }
                }
                return existing.Asset as T;
            }

            ResourceEntry entry = CreateEntry(path, managedKey);
            _entries.Add(managedKey, entry);

            try
            {
                AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(path);
                entry.Handle = handle;
                entry.LoadTask = AwaitHandle(entry, handle);
                await entry.LoadTask;
                return entry.Asset as T;
            }
            catch (Exception exception)
            {
                FailEntry(entry, exception);
                return null;
            }
        }

        private async Task<UnityEngine.Object> AwaitHandle<T>(
            ResourceEntry entry,
            AsyncOperationHandle<T> handle) where T : UnityEngine.Object
        {
            try
            {
                await handle.Task;
                return CompleteLoad(entry, handle.Result);
            }
            catch (Exception exception)
            {
                FailEntry(entry, exception);
                return null;
            }
        }

        /// <summary>释放所有资源。正在加载的资源会在加载完成后立即释放。</summary>
        public void ReleaseAll()
        {
            List<string> keys = ListPool<string>.Get();
            foreach (KeyValuePair<string, ResourceEntry> pair in _entries)
                keys.Add(pair.Key);

            for (int i = 0; i < keys.Count; i++)
            {
                if (!_entries.TryGetValue(keys[i], out ResourceEntry entry))
                    continue;

                entry.ReferenceCount = 0;
                if (!entry.IsLoading)
                    ReleaseEntry(entry);
            }
            ListPool<string>.Release(keys);
        }

        /// <summary>
        /// 生成管理器内部使用的 key。调用方只需要提供原始字符串。
        /// </summary>
        internal string BuildKey<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            System.Text.StringBuilder builder = StringBuilderPool.Get();
            builder.Append(key).Append('_').Append(typeof(T).Name);
            string managedKey = builder.ToString();
            StringBuilderPool.Release(builder);
            return managedKey;
        }

        /// <summary>获取已完成加载的资源，不增加引用计数。</summary>
        internal T GetLoaded<T>(string managedKey) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(managedKey) ||
                !_entries.TryGetValue(managedKey, out ResourceEntry entry) ||
                entry.IsLoading)
            {
                return null;
            }

            return entry.Asset as T;
        }

        /// <summary>
        /// 等待组内已经登记的加载完成，不增加管理器引用计数。
        /// 资源组用此方法处理同一组内的并发重复访问。
        /// </summary>
        internal T WaitForLoaded<T>(string managedKey) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(managedKey) ||
                !_entries.TryGetValue(managedKey, out ResourceEntry entry))
            {
                return null;
            }

            if (entry.IsLoading)
            {
                // 同步调用不能阻塞 Unity 主线程等待 Task，优先使用 Addressables
                // 自身的同步完成接口；只有没有有效 handle 时才等待已有 Task。
                if (entry.Handle.IsValid())
                {
                    entry.Handle.WaitForCompletion();
                    if (entry.IsLoading)
                        CompleteLoad(entry, entry.Handle.Result as UnityEngine.Object);
                }
                else if (entry.LoadTask != null)
                    entry.LoadTask.GetAwaiter().GetResult();
            }

            return entry.Asset as T;
        }

        /// <summary>等待组内已经登记的加载完成，不增加管理器引用计数。</summary>
        internal async Task<T> WaitForLoadedAsync<T>(string managedKey) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(managedKey) ||
                !_entries.TryGetValue(managedKey, out ResourceEntry entry))
            {
                return null;
            }

            if (entry.IsLoading)
            {
                if (entry.LoadTask != null)
                    await entry.LoadTask;
                else if (entry.Handle.IsValid())
                {
                    entry.Handle.WaitForCompletion();
                    CompleteLoad(entry, entry.Handle.Result as UnityEngine.Object);
                }
            }

            return entry.Asset as T;
        }

        /// <summary>按管理器内部 key 释放一个引用。</summary>
        internal void ReleaseManagedKey(string managedKey)
        {
            if (string.IsNullOrEmpty(managedKey) ||
                !_entries.TryGetValue(managedKey, out ResourceEntry entry))
            {
                return;
            }

            if (entry.ReferenceCount <= 0)
                return;

            entry.ReferenceCount--;
            if (entry.ReferenceCount > 0)
                return;

            if (entry.IsLoading)
                return;   // 在途：完成后 CompleteLoad 按 ReferenceCount <= 0 立即释放

            ReleaseEntry(entry);
        }

        /// <summary>按管理器内部 key 列表释放引用。</summary>
        internal void ReleaseManagedKeys(IList<string> managedKeys)
        {
            if (managedKeys == null)
                return;

            for (int i = 0; i < managedKeys.Count; i++)
                ReleaseManagedKey(managedKeys[i]);
        }

        private ResourceEntry CreateEntry(string path, string managedKey)
        {
            return new ResourceEntry
            {
                Path = path,
                ManagedKey = managedKey,
                ReferenceCount = 1
            };
        }

        private bool ValidateArguments<T>(string path, string key, out string managedKey)
            where T : UnityEngine.Object
        {
            managedKey = null;
            if (string.IsNullOrEmpty(path))
            {
                ZLog.LogError("[AddressablesLoadManager] 资源 path 不能为空。");
                return false;
            }

            if (string.IsNullOrEmpty(key))
            {
                ZLog.LogError("[AddressablesLoadManager] 资源 key 不能为空。");
                return false;
            }

            managedKey = BuildKey<T>(key);
            return !string.IsNullOrEmpty(managedKey);
        }

        private UnityEngine.Object CompleteLoad(ResourceEntry entry, UnityEngine.Object asset)
        {
            if (!entry.IsLoading)
                return entry.Asset;   // 已完成过（含失败路径），幂等返回

            entry.IsLoading = false;

            bool succeeded = entry.Handle.Status == AsyncOperationStatus.Succeeded && asset != null;
            if (succeeded)
            {
                entry.Asset = asset;
            }
            else
            {
                ZLog.LogError($"[AddressablesLoadManager] 资源加载失败，path={entry.Path}, key={entry.ManagedKey}。");
            }

            // 计数 <= 0 覆盖了"加载中被 ReleaseAll / ReleaseManagedKey 减空"的全部情况
            bool releaseImmediately = !succeeded || entry.ReferenceCount <= 0;
            if (releaseImmediately)
            {
                ReleaseEntry(entry);
                return null;
            }

            return entry.Asset;
        }

        private void FailEntry(ResourceEntry entry, Exception exception)
        {
            if (!entry.IsLoading)
                return;   // 已完成/已失败（两个 catch 调用者可能先后到达），幂等

            ZLog.LogError($"[AddressablesLoadManager] 创建资源加载操作失败，path={entry.Path}, key={entry.ManagedKey}, exception={exception}");
            entry.IsLoading = false;
            ReleaseEntry(entry);
        }

        private void ReleaseEntry(ResourceEntry entry)
        {
            if (entry.Handle.IsValid())
                Addressables.Release(entry.Handle);

            if (_entries.TryGetValue(entry.ManagedKey, out ResourceEntry current) &&
                ReferenceEquals(current, entry))
            {
                _entries.Remove(entry.ManagedKey);
            }

            entry.Asset = null;
        }

        private sealed class ResourceEntry
        {
            public string Path;
            public string ManagedKey;
            public AsyncOperationHandle Handle;
            public UnityEngine.Object Asset;
            public int ReferenceCount;
            public bool IsLoading = true;
            public Task<UnityEngine.Object> LoadTask;
        }
    }
}
