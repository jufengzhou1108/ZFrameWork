using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace ZFrameWork
{
    public static class AddressablesHelper
    {
        private static readonly ConcurrentQueue<OrphanRecord> OrphanGroups = new();

        private static bool _flushHooked;

        private readonly struct OrphanRecord
        {
            public readonly AddressablesLoadManager Manager;
            public readonly string[] Keys;

            public OrphanRecord(AddressablesLoadManager manager, string[] keys)
            {
                Manager = manager;
                Keys = keys;
            }
        }

        internal static void RegisterOrphan(AddressablesLoadManager manager, string[] keys)
        {
            OrphanGroups.Enqueue(new OrphanRecord(manager, keys));
        }

        internal static void EnsureFlushHook()
        {
            if (_flushHooked)
                return;
            _flushHooked = true;
            PublicMono.Instance.AddUpdateAction(FlushOrphanGroups);
        }

        private static void FlushOrphanGroups()
        {
            while (OrphanGroups.TryDequeue(out OrphanRecord record))
            {
                ZLog.LogWarning(
                    $"[AddressablesHelper] 业务未显式释放资源组，析构兜底代为释放 {record.Keys.Length} 个资源引用。");
                record.Manager.ReleaseManagedKeys(record.Keys);
            }
        }
    }
}
