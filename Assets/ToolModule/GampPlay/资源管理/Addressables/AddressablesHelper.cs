using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace ZFrameWork
{
    /// <summary>
    /// 资源组的析构兜底：业务忘记释放资源组时，由组的终结器登记孤儿账本，
    /// 本类在主线程心跳（PublicMono.Update）上代为还账并警告。
    /// 终结线程不能碰 Unity API，因此递交只走线程安全队列，释放动作全部在主线程执行。
    /// </summary>
    public static class AddressablesHelper
    {
        /// <summary>待清理的孤儿组队列：终结线程写入，主线程消费。</summary>
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

        /// <summary>
        /// 登记一个未释放的孤儿组。由资源组的终结器调用（终结线程），
        /// 只打包数据入队，不做任何其他事。
        /// </summary>
        internal static void RegisterOrphan(AddressablesLoadManager manager, string[] keys)
        {
            OrphanGroups.Enqueue(new OrphanRecord(manager, keys));
        }

        /// <summary>主线程心跳挂接。在资源组构造函数（正常线程）调用，终结线程不碰 Unity API。</summary>
        internal static void EnsureFlushHook()
        {
            if (_flushHooked)
                return;
            _flushHooked = true;
            PublicMono.Instance.AddUpdateAction(FlushOrphanGroups);
        }

        /// <summary>在主线程消费孤儿组，代为还账并警告。由 PublicMono 每帧驱动。</summary>
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
