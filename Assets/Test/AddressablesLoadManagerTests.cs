using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZFrameWork.PlayTests
{
    /// <summary>
    /// AddressablesLoadManager 契约测试套件。
    /// 覆盖不依赖 Addressables 运行时的全部接缝：参数校验、key 生成、释放/查询的兜底路径。
    /// 真实加载路径（LoadAssetAsync）无法在此覆盖——工程未配置 Addressables，
    /// 调用会触发自动初始化并可能写入工程；已在末尾以具名 Skip 记录该缺口。
    /// </summary>
    public static class AddressablesLoadManagerTests
    {
        public static async Task RunAll()
        {
            TestHarness.Reset();
            try
            {
                // ---- 冒烟 / 基本契约 ----
                await TestHarness.RunCaseAsync("BuildKey_AppendsTypeName", BuildKey_AppendsTypeName);
                await TestHarness.RunCaseAsync("BuildKey_NullOrEmptyKey_ReturnsNull", BuildKey_NullOrEmptyKey_ReturnsNull);

                // ---- 边界：参数校验（失败语义 = 返回 null + 记日志，不抛异常、不建条目）----
                await TestHarness.RunCaseAsync("Load_InvalidArguments_ReturnNullAndCreateNoEntry",
                    Load_InvalidArguments_ReturnNullAndCreateNoEntry);
                await TestHarness.RunCaseAsync("LoadAsync_InvalidArguments_ReturnNullAndCreateNoEntry",
                    LoadAsync_InvalidArguments_ReturnNullAndCreateNoEntry);

                // ---- 释放接缝：未知/空 key 一律幂等空操作 ----
                await TestHarness.RunCaseAsync("ReleaseManagedKey_UnknownOrInvalidKey_IsNoOp",
                    ReleaseManagedKey_UnknownOrInvalidKey_IsNoOp);
                await TestHarness.RunCaseAsync("ReleaseManagedKeys_NullAndUnknownList_IsNoOp",
                    ReleaseManagedKeys_NullAndUnknownList_IsNoOp);
                await TestHarness.RunCaseAsync("ReleaseAll_EmptyManager_IsIdempotentAndStaysUsable",
                    ReleaseAll_EmptyManager_IsIdempotentAndStaysUsable);

                // ---- 查询接缝：未知 key 一律返回 null（含曾被 HandleReleased 检查覆盖的场景）----
                await TestHarness.RunCaseAsync("GetLoaded_UnknownOrInvalidKey_ReturnsNull",
                    GetLoaded_UnknownOrInvalidKey_ReturnsNull);
                await TestHarness.RunCaseAsync("WaitForLoaded_UnknownOrInvalidKey_ReturnsNull",
                    WaitForLoaded_UnknownOrInvalidKey_ReturnsNull);
                await TestHarness.RunCaseAsync("WaitForLoadedAsync_UnknownOrInvalidKey_ReturnsNull",
                    WaitForLoadedAsync_UnknownOrInvalidKey_ReturnsNull);

                // ---- 压力：无效请求不建条目、不破坏管理器 ----
                await TestHarness.RunCaseAsync("Stress_MixedInvalidRequests_NoEntryLeakAndStaysUsable",
                    Stress_MixedInvalidRequests_NoEntryLeakAndStaysUsable);

                // ---- 无法覆盖的路径：显式记录，不假装通过 ----
                TestHarness.Skip("LoadAsset_RealAddressablesPath",
                    "工程未配置 Addressables（无 AddressableAssetsData），LoadAssetAsync 会触发自动初始化并可能写入工程；" +
                    "覆盖真实加载/失败/引用计数路径需要先配置 Addressables，或为管理器引入可注入的加载接缝");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[TEST][SUITE] AddressablesLoadManager：Play 模式已退出，剩余用例中止。");
                return;
            }

            TestHarness.Summary("AddressablesLoadManager");
        }

        // ---------- BuildKey ----------

        private static Task BuildKey_AppendsTypeName()
        {
            var manager = new AddressablesLoadManager();
            TestHarness.Require(manager.BuildKey<int>("hero") == "hero_Int32",
                $"key 应追加泛型类型名，实际 {manager.BuildKey<int>("hero")}");
            TestHarness.Require(manager.BuildKey<GameObject>("ui/popup") == "ui/popup_GameObject",
                "不同类型应产生不同 managedKey（同名字资源不会互相顶掉）");
            return Task.CompletedTask;
        }

        private static Task BuildKey_NullOrEmptyKey_ReturnsNull()
        {
            var manager = new AddressablesLoadManager();
            TestHarness.Require(manager.BuildKey<int>(null) == null, "null key 应返回 null");
            TestHarness.Require(manager.BuildKey<int>(string.Empty) == null, "空 key 应返回 null");
            return Task.CompletedTask;
        }

        // ---------- 参数校验：失败不建条目 ----------

        private static Task Load_InvalidArguments_ReturnNullAndCreateNoEntry()
        {
            var manager = new AddressablesLoadManager();

            // 预期 ZLog 报错（契约内诊断）
            TestHarness.Require(manager.Load<Object>("", "missing") == null, "空 path 同步加载应返回 null");
            TestHarness.Require(manager.Load<Object>("missing", "") == null, "空 key 同步加载应返回 null");
            TestHarness.Require(manager.Load<Object>(null, "missing") == null, "null path 同步加载应返回 null");
            TestHarness.Require(manager.Load<Object>("missing", null) == null, "null key 同步加载应返回 null");

            // 关键断言：无效请求不应创建任何条目（用查询接缝验证）
            TestHarness.Require(manager.GetLoaded<Object>("missing_Object") == null,
                "无效请求不应在字典中留下条目");
            TestHarness.Require(manager.WaitForLoaded<Object>("missing_Object") == null,
                "无效请求不应留下可等待的在途条目");
            return Task.CompletedTask;
        }

        private static async Task LoadAsync_InvalidArguments_ReturnNullAndCreateNoEntry()
        {
            var manager = new AddressablesLoadManager();

            TestHarness.Require(await manager.LoadAsync<Object>("", "missing") == null, "空 path 异步加载应返回 null");
            TestHarness.Require(await manager.LoadAsync<Object>("missing", "") == null, "空 key 异步加载应返回 null");
            TestHarness.Require(await manager.LoadAsync<Object>(null, "missing") == null, "null path 异步加载应返回 null");
            TestHarness.Require(await manager.LoadAsync<Object>("missing", null) == null, "null key 异步加载应返回 null");

            TestHarness.Require(manager.GetLoaded<Object>("missing_Object") == null,
                "无效异步请求不应在字典中留下条目");
        }

        // ---------- 释放接缝 ----------

        private static Task ReleaseManagedKey_UnknownOrInvalidKey_IsNoOp()
        {
            var manager = new AddressablesLoadManager();

            manager.ReleaseManagedKey("unknown_Object");   // 未加载过 → 空操作
            manager.ReleaseManagedKey(null);
            manager.ReleaseManagedKey(string.Empty);

            // 防回归：简化前该路径会写 ReleaseWhenLoaded 欠条字段；如今应完全无副作用
            TestHarness.Require(manager.BuildKey<int>("after-release") == "after-release_Int32",
                "释放未知 key 后管理器应保持可用");
            TestHarness.Require(manager.GetLoaded<Object>("unknown_Object") == null,
                "释放未知 key 不应产生条目");
            return Task.CompletedTask;
        }

        private static Task ReleaseManagedKeys_NullAndUnknownList_IsNoOp()
        {
            var manager = new AddressablesLoadManager();
            manager.ReleaseManagedKeys(null);   // 应直接返回，不抛异常

            var keys = new List<string> { "a_Object", null, string.Empty, "b_Object" };
            manager.ReleaseManagedKeys(keys);

            TestHarness.Require(manager.BuildKey<int>("after-release") == "after-release_Int32",
                "批量释放未知 key 后管理器应保持可用");
            return Task.CompletedTask;
        }

        private static Task ReleaseAll_EmptyManager_IsIdempotentAndStaysUsable()
        {
            var manager = new AddressablesLoadManager();
            manager.ReleaseAll();
            manager.ReleaseAll();
            manager.ReleaseAll();

            TestHarness.Require(manager.BuildKey<int>("after-release") == "after-release_Int32",
                "重复 ReleaseAll 后管理器应保持可用");
            TestHarness.Require(manager.GetLoaded<Object>("after-release_Object") == null,
                "ReleaseAll 不应留下条目");
            return Task.CompletedTask;
        }

        // ---------- 查询接缝 ----------

        private static Task GetLoaded_UnknownOrInvalidKey_ReturnsNull()
        {
            var manager = new AddressablesLoadManager();

            TestHarness.Require(manager.GetLoaded<Object>("never-loaded_Object") == null,
                "未加载过的 key 应返回 null（条目不在字典中 = 已释放）");
            TestHarness.Require(manager.GetLoaded<Object>(null) == null, "null managedKey 应返回 null");
            TestHarness.Require(manager.GetLoaded<Object>(string.Empty) == null, "空 managedKey 应返回 null");
            return Task.CompletedTask;
        }

        private static Task WaitForLoaded_UnknownOrInvalidKey_ReturnsNull()
        {
            var manager = new AddressablesLoadManager();

            TestHarness.Require(manager.WaitForLoaded<Object>("never-loaded_Object") == null,
                "等待未加载的 key 应返回 null，且不阻塞");
            TestHarness.Require(manager.WaitForLoaded<Object>(null) == null, "null managedKey 应返回 null");
            TestHarness.Require(manager.WaitForLoaded<Object>(string.Empty) == null, "空 managedKey 应返回 null");
            return Task.CompletedTask;
        }

        private static async Task WaitForLoadedAsync_UnknownOrInvalidKey_ReturnsNull()
        {
            var manager = new AddressablesLoadManager();

            TestHarness.Require(await manager.WaitForLoadedAsync<Object>("never-loaded_Object") == null,
                "异步等待未加载的 key 应返回 null");
            TestHarness.Require(await manager.WaitForLoadedAsync<Object>(null) == null, "null managedKey 应返回 null");
            TestHarness.Require(await manager.WaitForLoadedAsync<Object>(string.Empty) == null, "空 managedKey 应返回 null");
        }

        // ---------- 压力 ----------

        private static async Task Stress_MixedInvalidRequests_NoEntryLeakAndStaysUsable()
        {
            const int rounds = 256;
            var manager = new AddressablesLoadManager();
            var keys = new List<string> { "stress_Object", "other_Object" };

            for (int i = 0; i < rounds; i++)
            {
                manager.ReleaseManagedKey("stress_Object");
                manager.ReleaseManagedKeys(keys);
                TestHarness.Require(manager.GetLoaded<Object>("stress_Object") == null,
                    $"第 {i} 轮：不应存在条目");
                TestHarness.Require(manager.WaitForLoaded<Object>("stress_Object") == null,
                    $"第 {i} 轮：不应存在可等待的在途条目");
                TestHarness.Require(await manager.LoadAsync<Object>("missing", "") == null,
                    $"第 {i} 轮：无效参数应返回 null");
                manager.ReleaseAll();
            }

            TestHarness.Require(manager.BuildKey<int>("after-stress") == "after-stress_Int32",
                "压力后管理器应保持可用");
            TestHarness.Require(manager.GetLoaded<Object>("stress_Object") == null,
                "压力后不应残留条目");
        }
    }
}
