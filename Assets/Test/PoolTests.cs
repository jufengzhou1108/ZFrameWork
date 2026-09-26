using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZFrameWork.PlayTests
{
    /// <summary>
    /// 对象池模块（Pool / PoolCallbackFactory / 池化集合）的 Play 模式测试套件。
    /// 本轮改动重点在"归还即默认"这条链路：默认化回调的三种来源（注入 / 注入 null 回落工厂 / 工厂登记），
    /// 以及集合池把清空职责从 wrapper 搬进池之后语义是否不变。
    /// 标注"预期诊断"的用例会主动触发一次 ZLog 报错，报错是契约的一部分，断言一律落在状态上。
    /// </summary>
    public static class PoolTests
    {
        public static void RunAll()
        {
            TestHarness.Reset();
            try
            {
                // 冒烟 / 基本契约
                TestHarness.RunCase("Get_EmptyPool_CreatesEachTime", Get_EmptyPool_CreatesEachTime);
                TestHarness.RunCase("Add_ThenGet_ReusesSameInstanceWithoutCreating", Add_ThenGet_ReusesSameInstanceWithoutCreating);

                // 边界：归还侧
                TestHarness.RunCase("Add_Null_LogsErrorAndIsSkipped", Add_Null_LogsErrorAndIsSkipped);
                TestHarness.RunCase("Add_Duplicate_LogsErrorAndKeepsSingle", Add_Duplicate_LogsErrorAndKeepsSingle);
                TestHarness.RunCase("Add_AtCapacity_IsRejectedSilently", Add_AtCapacity_IsRejectedSilently);
                TestHarness.RunCase("Add_AfterGet_TrackedPairingAllowsReAdd", Add_AfterGet_TrackedPairingAllowsReAdd);

                // 边界：取出侧与清理
                TestHarness.RunCase("Get_CreateReturnsNull_LogsErrorAndReturnsNull", Get_CreateReturnsNull_LogsErrorAndReturnsNull);
                TestHarness.RunCase("Clear_DestroysFreeObjectsAndEmpties", Clear_DestroysFreeObjectsAndEmpties);
                TestHarness.RunCase("CleanExpired_Expired_InvokesDestroy", CleanExpired_Expired_InvokesDestroy);
                TestHarness.RunCase("CleanExpired_NotYetExpired_KeepsObjects", CleanExpired_NotYetExpired_KeepsObjects);
                TestHarness.RunCase("CleanExpired_ExpireTimeDisabled_DoesNothing", CleanExpired_ExpireTimeDisabled_DoesNothing);

                // 本轮核心：归还即默认
                TestHarness.RunCase("Add_InjectedResetAction_RunsOnSameInstance", Add_InjectedResetAction_RunsOnSameInstance);
                TestHarness.RunCase("Add_ResetInjectedNull_LogsErrorAndFallsBackToFactory", Add_ResetInjectedNull_LogsErrorAndFallsBackToFactory);
                TestHarness.RunCase("Add_NoInjection_UsesFactoryRegisteredReset", Add_NoInjection_UsesFactoryRegisteredReset);
                TestHarness.RunCase("PoolCallbackFactory_UnregisteredType_ReturnsSameEmptyDelegate", PoolCallbackFactory_UnregisteredType_ReturnsSameEmptyDelegate);
                TestHarness.RunCase("PoolCallbackFactory_RegisterDuplicate_LogsErrorAndKeepsFirst", PoolCallbackFactory_RegisterDuplicate_LogsErrorAndKeepsFirst);
                TestHarness.RunCase("PoolCallbackFactory_RegisterNull_LogsErrorAndStaysUnregistered", PoolCallbackFactory_RegisterNull_LogsErrorAndStaysUnregistered);

                // 集合池：清空职责搬进池之后的语义
                TestHarness.RunCase("CollectionPools_SequencePools_ReleaseThenGet_AreEmpty", CollectionPools_SequencePools_ReleaseThenGet_AreEmpty);
                TestHarness.RunCase("CollectionPools_MapPools_ReleaseThenGet_AreEmpty", CollectionPools_MapPools_ReleaseThenGet_AreEmpty);
                TestHarness.RunCase("StringBuilderPool_ReleaseThenGet_IsEmpty", StringBuilderPool_ReleaseThenGet_IsEmpty);
                TestHarness.RunCase("ListPool_ReleaseNull_LogsErrorAndDoesNotThrow", ListPool_ReleaseNull_LogsErrorAndDoesNotThrow);

                TestHarness.Skip("PoolManager_RegisterAndClear",
                    "PoolManager 全工程零引用，且它的注册/清理语义与本轮改动无关，等真实使用方出现再补");
                TestHarness.Skip("Pool_ThreadSafety",
                    "对象池不做线程安全保证（主线程专用约定），无法也不应在此验证并发行为");
                TestHarness.Skip("Regression_NoRecordedBugs",
                    "尚无历史缺陷用例；发现缺陷后在此保留具名回归测试");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TEST][FAIL] 套件级异常: {e}");
            }

            TestHarness.Summary("Pool");
        }

        // ---------- 冒烟 / 基本契约 ----------

        private static void Get_EmptyPool_CreatesEachTime()
        {
            int created = 0;
            var pool = new Pool<PooledThing>(() =>
            {
                created++;
                return new PooledThing();
            }, null, _ => { });

            PooledThing first = pool.Get();
            PooledThing second = pool.Get();

            TestHarness.Require(first != null && second != null, "池空时 Get 应通过 createFunc 造出对象");
            TestHarness.Require(created == 2, $"池空时每次 Get 都应新建，实际创建 {created} 次");
            TestHarness.Require(!ReferenceEquals(first, second), "两次 Get 应拿到不同实例");
            TestHarness.Require(pool.Count == 0, $"取出的对象不计入空闲数，实际 Count={pool.Count}");
        }

        private static void Add_ThenGet_ReusesSameInstanceWithoutCreating()
        {
            int created = 0;
            var pool = new Pool<PooledThing>(() =>
            {
                created++;
                return new PooledThing();
            }, null, _ => { });

            var thing = new PooledThing { Value = 7 };
            pool.Add(thing);
            TestHarness.Require(pool.Count == 1, $"归还后池中应有 1 个空闲对象，实际 {pool.Count}");

            PooledThing got = pool.Get();
            TestHarness.Require(ReferenceEquals(got, thing), "应从池中取回同一个实例");
            TestHarness.Require(created == 0, $"池中有空闲时不应新建对象，实际新建 {created} 次");
            TestHarness.Require(pool.Count == 0, $"取出后空闲数应回到 0，实际 {pool.Count}");
        }

        // ---------- 边界：归还侧 ----------

        /// <summary>预期诊断：归还 null 打一条报错。</summary>
        private static void Add_Null_LogsErrorAndIsSkipped()
        {
            var pool = new Pool<PooledThing>(() => new PooledThing(), null, _ => { });

            pool.Add(null); // 预期诊断

            TestHarness.Require(pool.Count == 0, $"null 不应进入池中，实际 Count={pool.Count}");
        }

        /// <summary>预期诊断：重复归还打一条报错。</summary>
        private static void Add_Duplicate_LogsErrorAndKeepsSingle()
        {
            var pool = new Pool<PooledThing>(() => new PooledThing(), null, _ => { });
            var thing = new PooledThing();

            pool.Add(thing);
            pool.Add(thing); // 预期诊断

            TestHarness.Require(pool.Count == 1, $"重复归还只应保留一份，实际 Count={pool.Count}");
        }

        /// <summary>满池拒绝是既定语义（入池与归还者无关，不反馈），这里只钉住"入不了池且不抛异常"。</summary>
        private static void Add_AtCapacity_IsRejectedSilently()
        {
            var pool = new Pool<PooledThing>(() => new PooledThing(), null, _ => { })
            {
                Capacity = 1,
            };

            pool.Add(new PooledThing());
            pool.Add(new PooledThing()); // 超上限，按语义静默拒绝

            TestHarness.Require(pool.Count == 1, $"超出容量上限的归还不应入池，实际 Count={pool.Count}");
            TestHarness.Require(pool.Capacity == 1, $"容量上限应保持为 1，实际 {pool.Capacity}");
        }

        private static void Add_AfterGet_TrackedPairingAllowsReAdd()
        {
            var pool = new Pool<PooledThing>(() => new PooledThing(), null, _ => { });
            var thing = new PooledThing();

            pool.Add(thing);
            pool.Get();
            pool.Add(thing); // 取回后再归还，不应被误判为重复归还

            TestHarness.Require(pool.Count == 1, $"取出后的对象应能重新入池，实际 Count={pool.Count}");
        }

        // ---------- 边界：取出侧与清理 ----------

        /// <summary>预期诊断：createFunc 返回 null 打一条报错。</summary>
        private static void Get_CreateReturnsNull_LogsErrorAndReturnsNull()
        {
            var pool = new Pool<PooledThing>(() => null, null, _ => { });

            PooledThing got = pool.Get(); // 预期诊断

            TestHarness.Require(got == null, "createFunc 返回 null 时 Get 应返回 null");
        }

        private static void Clear_DestroysFreeObjectsAndEmpties()
        {
            int destroyed = 0;
            var pool = new Pool<PooledThing>(() => new PooledThing(), _ => destroyed++, _ => { });

            pool.Add(new PooledThing());
            pool.Add(new PooledThing());
            pool.Clear();

            TestHarness.Require(destroyed == 2, $"清空应逐个销毁空闲对象，实际销毁 {destroyed} 个");
            TestHarness.Require(pool.Count == 0, $"清空后不应有剩余，实际 {pool.Count}");
        }

        private static void CleanExpired_Expired_InvokesDestroy()
        {
            int destroyed = 0;
            var pool = new FakeClockPool(() => new PooledThing(), _ => destroyed++, _ => { })
            {
                ExpireTime = 10f,
                Now = 0f,
            };
            pool.Add(new PooledThing());

            pool.Now = 11f;
            pool.CleanExpired();

            TestHarness.Require(destroyed == 1, $"过期对象应被销毁，实际销毁 {destroyed} 个");
            TestHarness.Require(pool.Count == 0, $"过期清理后空闲数应为 0，实际 {pool.Count}");
        }

        private static void CleanExpired_NotYetExpired_KeepsObjects()
        {
            int destroyed = 0;
            var pool = new FakeClockPool(() => new PooledThing(), _ => destroyed++, _ => { })
            {
                ExpireTime = 10f,
                Now = 0f,
            };
            pool.Add(new PooledThing());

            pool.Now = 5f;
            pool.CleanExpired();

            TestHarness.Require(destroyed == 0, $"未到期的对象不应被销毁，实际销毁 {destroyed} 个");
            TestHarness.Require(pool.Count == 1, $"未到期的对象应保留，实际 Count={pool.Count}");
        }

        private static void CleanExpired_ExpireTimeDisabled_DoesNothing()
        {
            int destroyed = 0;
            var pool = new FakeClockPool(() => new PooledThing(), _ => destroyed++, _ => { })
            {
                Now = 0f,
            };
            pool.Add(new PooledThing());

            pool.Now = 100000f; // 未启用过期（ExpireTime <= 0）
            pool.CleanExpired();

            TestHarness.Require(destroyed == 0 && pool.Count == 1,
                $"未启用过期清理时不应销毁任何对象，实际销毁 {destroyed} 个、Count={pool.Count}");
        }

        // ---------- 本轮核心：归还即默认 ----------

        private static void Add_InjectedResetAction_RunsOnSameInstance()
        {
            var resetTargets = new List<PooledThing>();
            var pool = new Pool<PooledThing>(
                () => new PooledThing(),
                destroyAction: null,
                resetAction: thing =>
                {
                    resetTargets.Add(thing);
                    thing.Value = 0;
                });

            var thing = new PooledThing { Value = 99 };
            pool.Add(thing);

            TestHarness.Require(resetTargets.Count == 1, $"归还时应调用一次注入的默认化回调，实际 {resetTargets.Count} 次");
            TestHarness.Require(ReferenceEquals(resetTargets[0], thing), "默认化回调收到的应是同一个实例");
            TestHarness.Require(thing.Value == 0, $"默认化后对象应回到默认状态，实际 Value={thing.Value}");
        }

        /// <summary>预期诊断：三参构造显式传 null 打一条报错，然后回落到工厂。</summary>
        private static void Add_ResetInjectedNull_LogsErrorAndFallsBackToFactory()
        {
            bool factoryCalled = false;
            PoolCallbackFactory.Instance.Register<FactoryThingA>(thing =>
            {
                factoryCalled = true;
                thing.Value = 0;
            });

            var pool = new Pool<FactoryThingA>(
                () => new FactoryThingA(),
                destroyAction: null,
                resetAction: null); // 预期诊断：显式注入 null

            var thing = new FactoryThingA { Value = 5 };
            pool.Add(thing);

            TestHarness.Require(factoryCalled, "注入 null 时应回落到工厂登记的回调");
            TestHarness.Require(thing.Value == 0, $"回落后的默认化也应生效，实际 Value={thing.Value}");
        }

        private static void Add_NoInjection_UsesFactoryRegisteredReset()
        {
            var resetTargets = new List<FactoryThingB>();
            PoolCallbackFactory.Instance.Register<FactoryThingB>(thing =>
            {
                resetTargets.Add(thing);
                thing.Value = 0;
            });

            // 两参构造：不注入默认化回调，规则完全来自工厂
            var pool = new Pool<FactoryThingB>(() => new FactoryThingB());

            var thing = new FactoryThingB { Value = 8 };
            pool.Add(thing);

            TestHarness.Require(resetTargets.Count == 1,
                $"未注入时应使用工厂登记的默认化回调，实际调用 {resetTargets.Count} 次");
            TestHarness.Require(ReferenceEquals(resetTargets[0], thing), "工厂回调收到的应是同一个实例");
        }

        private static void PoolCallbackFactory_UnregisteredType_ReturnsSameEmptyDelegate()
        {
            Action<FactoryThingUnregistered> first = PoolCallbackFactory.Instance.Get<FactoryThingUnregistered>();
            Action<FactoryThingUnregistered> second = PoolCallbackFactory.Instance.Get<FactoryThingUnregistered>();

            TestHarness.Require(first != null && second != null, "未注册的类型也应拿到可调用的委托（空委托兜底）");
            TestHarness.Require(ReferenceEquals(first, second),
                "空委托应按类型缓存，两次查询应拿到同一实例（否则每次查询都在分配）");

            var thing = new FactoryThingUnregistered { Value = 3 };
            first(thing); // 空委托，调用应无副作用、不抛异常
            TestHarness.Require(thing.Value == 3, "空委托不应改动对象");
        }

        /// <summary>预期诊断：重复注册打一条报错。</summary>
        private static void PoolCallbackFactory_RegisterDuplicate_LogsErrorAndKeepsFirst()
        {
            bool secondCalled = false;
            PoolCallbackFactory.Instance.Register<FactoryThingC>(_ => { });
            PoolCallbackFactory.Instance.Register<FactoryThingC>(_ => secondCalled = true); // 预期诊断

            Action<FactoryThingC> callback = PoolCallbackFactory.Instance.Get<FactoryThingC>();
            callback(new FactoryThingC());

            TestHarness.Require(!secondCalled, "重复注册应被拒绝，先注册的那个必须仍然生效");
        }

        /// <summary>预期诊断：注册 null 打一条报错。</summary>
        private static void PoolCallbackFactory_RegisterNull_LogsErrorAndStaysUnregistered()
        {
            PoolCallbackFactory.Instance.Register<PooledThing>(null); // 预期诊断

            Action<PooledThing> callback = PoolCallbackFactory.Instance.Get<PooledThing>();
            var thing = new PooledThing { Value = 4 };
            callback(thing);

            TestHarness.Require(thing.Value == 4, "注册 null 被拒绝后该类型应仍处于未注册状态（拿到的是空委托）");
        }

        // ---------- 集合池 ----------

        private static void CollectionPools_SequencePools_ReleaseThenGet_AreEmpty()
        {
            var list = ListPool<int>.Get();
            list.Add(1);
            ListPool<int>.Release(list);
            TestHarness.Require(ListPool<int>.Get().Count == 0, "ListPool 归还即清空");

            var queue = QueuePool<int>.Get();
            queue.Enqueue(1);
            QueuePool<int>.Release(queue);
            TestHarness.Require(QueuePool<int>.Get().Count == 0, "QueuePool 归还即清空");

            var stack = StackPool<int>.Get();
            stack.Push(1);
            StackPool<int>.Release(stack);
            TestHarness.Require(StackPool<int>.Get().Count == 0, "StackPool 归还即清空");

            var linked = LinkedListPool<int>.Get();
            linked.AddLast(1);
            LinkedListPool<int>.Release(linked);
            TestHarness.Require(LinkedListPool<int>.Get().Count == 0, "LinkedListPool 归还即清空");
        }

        private static void CollectionPools_MapPools_ReleaseThenGet_AreEmpty()
        {
            var dictionary = DictionaryPool<int, int>.Get();
            dictionary[1] = 1;
            DictionaryPool<int, int>.Release(dictionary);
            TestHarness.Require(DictionaryPool<int, int>.Get().Count == 0, "DictionaryPool 归还即清空");

            var hashSet = HashSetPool<int>.Get();
            hashSet.Add(1);
            HashSetPool<int>.Release(hashSet);
            TestHarness.Require(HashSetPool<int>.Get().Count == 0, "HashSetPool 归还即清空");
        }

        private static void StringBuilderPool_ReleaseThenGet_IsEmpty()
        {
            var builder = StringBuilderPool.Get();
            builder.Append("x");
            StringBuilderPool.Release(builder);

            TestHarness.Require(StringBuilderPool.Get().Length == 0, "StringBuilderPool 归还即清空");
        }

        /// <summary>预期诊断：归还 null 打一条报错（由 Pool.Add 的 null 检查给出）。</summary>
        private static void ListPool_ReleaseNull_LogsErrorAndDoesNotThrow()
        {
            ListPool<int>.Release(null); // 预期诊断

            // 容器未被污染：正常路径仍可用
            var list = ListPool<int>.Get();
            TestHarness.Require(list != null && list.Count == 0, "归还 null 不应影响池的后续使用");
            ListPool<int>.Release(list);
        }
    }
}
