using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;

namespace ZFrameWork
{
    /// <summary>
    /// 对象池 / 池化集合 / 置空工厂：只走公开面。
    /// 不为生产代码加卸载 API。工厂表不能反注册，类型只 Register 一次语义的用例必须能在第二遍套件下仍成立。
    /// </summary>
    public static class PoolTests
    {
        public static async UniTask RunAll()
        {
            TestHarness.Reset();

            TestHarness.RunCase("Smoke_PoolGetAddGet_ReturnsSameInstance", Smoke_PoolGetAddGet_ReturnsSameInstance);
            TestHarness.RunCase("Smoke_PoolAdd_ExplicitResetClearsValue", Smoke_PoolAdd_ExplicitResetClearsValue);
            TestHarness.RunCase("Smoke_ListPoolGetRelease_ClearsContents", Smoke_ListPoolGetRelease_ClearsContents);
            TestHarness.RunCase("Smoke_ListPool_InheritsPool", Smoke_ListPool_InheritsPool);

            TestHarness.RunCase("Boundary_CreateFuncNull_GetReturnsNull", Boundary_CreateFuncNull_GetReturnsNull);
            TestHarness.RunCase("Boundary_AddNull_CountUnchanged", Boundary_AddNull_CountUnchanged);
            TestHarness.RunCase("Boundary_CapacityZero_RejectsAdd", Boundary_CapacityZero_RejectsAdd);
            TestHarness.RunCase("Boundary_CapacityNegative_BecomesZero", Boundary_CapacityNegative_BecomesZero);
            TestHarness.RunCase("Boundary_FactoryUnregistered_ResetIsNoOp", Boundary_FactoryUnregistered_ResetIsNoOp);
            TestHarness.RunCase("Boundary_ListPoolReleaseNull_ExpectedDiagnostic", Boundary_ListPoolReleaseNull_ExpectedDiagnostic);

            TestHarness.RunCase("Lifecycle_Clear_CountZeroAndDestroyCalled", Lifecycle_Clear_CountZeroAndDestroyCalled);
            TestHarness.RunCase("Lifecycle_GetAfterClear_CreatesNew", Lifecycle_GetAfterClear_CreatesNew);
            TestHarness.RunCase("Lifecycle_ExpireEnabled_IdleDestroyedOnGet", Lifecycle_ExpireEnabled_IdleDestroyedOnGet);
            TestHarness.RunCase("Lifecycle_ExpireDisabled_KeepsIdle", Lifecycle_ExpireDisabled_KeepsIdle);

            TestHarness.RunCase("Mutation_DuplicateAdd_SecondRejected", Mutation_DuplicateAdd_SecondRejected);
            TestHarness.RunCase("Mutation_ExplicitReset_OverridesFactory", Mutation_ExplicitReset_OverridesFactory);
            TestHarness.Skip(
                "Mutation_RegisterAfterPoolConstructed",
                "工厂不能反注册，第二遍套件会看到上一遍的 Register，无法稳定断言快照");

            TestHarness.RunCase("Failure_CreateReturnsNull_GetReturnsNull", Failure_CreateReturnsNull_GetReturnsNull);
            TestHarness.RunCase("Failure_ThreeArgNullReset_FallsBackToFactory", Failure_ThreeArgNullReset_FallsBackToFactory);
            TestHarness.RunCase("Failure_RegisterNull_DoesNotReplace", Failure_RegisterNull_DoesNotReplace);
            TestHarness.RunCase("Failure_RegisterDuplicate_KeepsFirstCallback", Failure_RegisterDuplicate_KeepsFirstCallback);

            TestHarness.RunCase("Stress_PoolGetAdd256_CountMatches", Stress_PoolGetAdd256_CountMatches);
            TestHarness.RunCase("Stress_ListPoolGetRelease64_AlwaysEmpty", Stress_ListPoolGetRelease64_AlwaysEmpty);

            TestHarness.RunCase("Contract_DictionaryPool_ReleaseClears", Contract_DictionaryPool_ReleaseClears);
            TestHarness.RunCase("Contract_HashSetPool_ReleaseClears", Contract_HashSetPool_ReleaseClears);
            TestHarness.RunCase("Contract_LinkedListPool_ReleaseClears", Contract_LinkedListPool_ReleaseClears);
            TestHarness.RunCase("Contract_QueuePool_ReleaseClears", Contract_QueuePool_ReleaseClears);
            TestHarness.RunCase("Contract_StackPool_ReleaseClears", Contract_StackPool_ReleaseClears);
            TestHarness.RunCase("Contract_StringBuilderPool_ReleaseClears", Contract_StringBuilderPool_ReleaseClears);
            TestHarness.RunCase("Contract_TwoArgPool_UsesFactoryReset", Contract_TwoArgPool_UsesFactoryReset);

            TestHarness.RunCase("Regression_ListPoolReuse_DoesNotKeepOldItems", Regression_ListPoolReuse_DoesNotKeepOldItems);

            TestHarness.RunCase("Integration_PoolManager_RegisterGetRemoveClear", Integration_PoolManager_RegisterGetRemoveClear);
            TestHarness.RunCase("Integration_ListActionInvoke_SurvivesHashSetPool", Integration_ListActionInvoke_SurvivesHashSetPool);

            TestHarness.Summary("PoolTests");
            await UniTask.CompletedTask;
        }

        private sealed class Probe
        {
            public int Value;
        }

        private sealed class UnregisteredProbe
        {
            public int Value;
        }

        private sealed class FactoryProbe
        {
            public int Value;
        }

        private sealed class OverrideProbe
        {
            public int Value;
        }

        private sealed class FallbackProbe
        {
            public int Value;
        }

        private sealed class DupProbe
        {
            public int Value;
        }

        private sealed class NullCreateProbe
        {
        }

        private sealed class ExpireItem
        {
        }

        private sealed class ClockPool : Pool<ExpireItem>
        {
            public float Now;
            public int Destroyed;

            public ClockPool()
                : base(() => new ExpireItem(), _ => { })
            {
            }

            public ClockPool(Action<ExpireItem> destroy)
                : base(() => new ExpireItem(), destroy)
            {
            }

            protected override float GetCurrentTime()
            {
                return Now;
            }
        }

        private static Pool<Probe> NewExplicitPool(Action<Probe> reset = null)
        {
            return new Pool<Probe>(() => new Probe(), null, reset ?? (p => p.Value = 0));
        }

        private static void Smoke_PoolGetAddGet_ReturnsSameInstance()
        {
            Pool<Probe> pool = NewExplicitPool();
            Probe first = pool.Get();
            TestHarness.Require(first != null, "Get 应返回实例");
            pool.Add(first);
            TestHarness.Require(pool.Count == 1, $"归还后 Count 应为 1，实际 {pool.Count}");
            Probe second = pool.Get();
            TestHarness.Require(ReferenceEquals(first, second), "池空闲时应归还同一实例");
            TestHarness.Require(pool.Count == 0, "借出后 Count 应为 0");
        }

        private static void Smoke_PoolAdd_ExplicitResetClearsValue()
        {
            Pool<Probe> pool = NewExplicitPool();
            Probe probe = pool.Get();
            probe.Value = 7;
            pool.Add(probe);
            Probe again = pool.Get();
            TestHarness.Require(again.Value == 0, $"显式置空应在归还时执行，实际 {again.Value}");
        }

        private static void Smoke_ListPoolGetRelease_ClearsContents()
        {
            List<int> list = null;
            try
            {
                list = ListPool<int>.Get();
                list.Add(1);
                list.Add(2);
                ListPool<int>.Release(list);
                list = ListPool<int>.Get();
                TestHarness.Require(list.Count == 0, $"ListPool 归还应 Clear，实际 Count={list.Count}");
            }
            finally
            {
                if (list != null)
                {
                    ListPool<int>.Release(list);
                }

                ListPool<int>.Instance.Clear();
            }
        }

        private static void Smoke_ListPool_InheritsPool()
        {
            TestHarness.Require(ListPool<int>.Instance is Pool<List<int>>, "ListPool 应继承 Pool<List<T>>");
            TestHarness.Require(ListPool<int>.Instance is IPool<List<int>>, "ListPool 应实现 IPool<List<T>>");
        }

        private static void Boundary_CreateFuncNull_GetReturnsNull()
        {
            Pool<Probe> pool = new Pool<Probe>(null);
            Probe probe = pool.Get();
            TestHarness.Require(probe == null, "createFunc 为 null 时 Get 应返回 null");
        }

        private static void Boundary_AddNull_CountUnchanged()
        {
            Pool<Probe> pool = NewExplicitPool();
            pool.Add(null);
            TestHarness.Require(pool.Count == 0, "Add(null) 应被跳过");
        }

        private static void Boundary_CapacityZero_RejectsAdd()
        {
            Pool<Probe> pool = NewExplicitPool();
            pool.Capacity = 0;
            Probe probe = pool.Get();
            pool.Add(probe);
            TestHarness.Require(pool.Count == 0, "Capacity=0 时应拒绝入池");
            Probe again = pool.Get();
            TestHarness.Require(!ReferenceEquals(probe, again), "未入池的对象不应被再次借出");
        }

        private static void Boundary_CapacityNegative_BecomesZero()
        {
            Pool<Probe> pool = NewExplicitPool();
            pool.Capacity = -3;
            TestHarness.Require(pool.Capacity == 0, $"负容量应钳到 0，实际 {pool.Capacity}");
        }

        private static void Boundary_FactoryUnregistered_ResetIsNoOp()
        {
            Pool<UnregisteredProbe> pool = new Pool<UnregisteredProbe>(() => new UnregisteredProbe(), null);
            UnregisteredProbe probe = pool.Get();
            probe.Value = 9;
            pool.Add(probe);
            UnregisteredProbe again = pool.Get();
            TestHarness.Require(again.Value == 9, $"未注册类型应拿到空委托，值应保留，实际 {again.Value}");
        }

        private static void Boundary_ListPoolReleaseNull_ExpectedDiagnostic()
        {
            int before = ListPool<int>.Instance.Count;
            ListPool<int>.Release(null);
            TestHarness.Require(ListPool<int>.Instance.Count == before, "Release(null) 不应改变池 Count");
        }

        private static void Lifecycle_Clear_CountZeroAndDestroyCalled()
        {
            int destroyed = 0;
            Pool<Probe> pool = new Pool<Probe>(() => new Probe(), _ => destroyed++, p => p.Value = 0);
            Probe a = pool.Get();
            Probe b = pool.Get();
            pool.Add(a);
            pool.Add(b);
            TestHarness.Require(pool.Count == 2, "应有 2 个空闲对象");
            pool.Clear();
            TestHarness.Require(pool.Count == 0, "Clear 后 Count 应为 0");
            TestHarness.Require(destroyed == 2, $"Clear 应调用 destroy，实际 {destroyed}");
        }

        private static void Lifecycle_GetAfterClear_CreatesNew()
        {
            Pool<Probe> pool = NewExplicitPool();
            Probe first = pool.Get();
            pool.Add(first);
            pool.Clear();
            Probe second = pool.Get();
            TestHarness.Require(!ReferenceEquals(first, second), "Clear 后应新建，不应借到已销毁的空闲对象");
        }

        private static void Lifecycle_ExpireEnabled_IdleDestroyedOnGet()
        {
            int destroyed = 0;
            ClockPool pool = new ClockPool(_ => destroyed++);
            pool.ExpireTime = 1f;
            pool.Now = 0f;
            ExpireItem item = pool.Get();
            pool.Add(item);
            TestHarness.Require(pool.Count == 1, "刚归还应在池中");
            pool.Now = 2f;
            ExpireItem next = pool.Get();
            TestHarness.Require(destroyed == 1, $"过期空闲对象应被销毁，实际 {destroyed}");
            TestHarness.Require(!ReferenceEquals(item, next), "过期后 Get 应拿到新实例");
        }

        private static void Lifecycle_ExpireDisabled_KeepsIdle()
        {
            ClockPool pool = new ClockPool();
            pool.ExpireTime = -1f;
            pool.Now = 0f;
            ExpireItem item = pool.Get();
            pool.Add(item);
            pool.Now = 100f;
            ExpireItem next = pool.Get();
            TestHarness.Require(ReferenceEquals(item, next), "未启用过期时应复用");
        }

        private static void Mutation_DuplicateAdd_SecondRejected()
        {
            Pool<Probe> pool = NewExplicitPool();
            Probe probe = pool.Get();
            pool.Add(probe);
            TestHarness.Require(pool.Count == 1, "第一次归还应入池");
            pool.Add(probe);
            TestHarness.Require(pool.Count == 1, "重复归还应被拒绝");
        }

        private static void Mutation_ExplicitReset_OverridesFactory()
        {
            PoolCallbackFactory.Instance.Register<OverrideProbe>(p => p.Value = 1);
            Pool<OverrideProbe> pool = new Pool<OverrideProbe>(() => new OverrideProbe(), null, p => p.Value = 2);
            OverrideProbe probe = pool.Get();
            probe.Value = 9;
            pool.Add(probe);
            OverrideProbe again = pool.Get();
            TestHarness.Require(again.Value == 2, $"三参注入应优先于工厂，实际 {again.Value}");
        }

        private static void Failure_CreateReturnsNull_GetReturnsNull()
        {
            Pool<NullCreateProbe> pool = new Pool<NullCreateProbe>(() => null);
            TestHarness.Require(pool.Get() == null, "工厂返回 null 时 Get 应为 null");
        }

        private static void Failure_ThreeArgNullReset_FallsBackToFactory()
        {
            PoolCallbackFactory.Instance.Register<FallbackProbe>(p => p.Value = 0);
            Pool<FallbackProbe> pool = new Pool<FallbackProbe>(() => new FallbackProbe(), null, null);
            FallbackProbe probe = pool.Get();
            probe.Value = 4;
            pool.Add(probe);
            FallbackProbe again = pool.Get();
            TestHarness.Require(again.Value == 0, $"三参传 null 应回落工厂置空，实际 {again.Value}");
        }

        private static void Failure_RegisterNull_DoesNotReplace()
        {
            PoolCallbackFactory.Instance.Register<FactoryProbe>(p => p.Value = 0);
            PoolCallbackFactory.Instance.Register<FactoryProbe>(null);
            Pool<FactoryProbe> pool = new Pool<FactoryProbe>(() => new FactoryProbe(), null);
            FactoryProbe probe = pool.Get();
            probe.Value = 5;
            pool.Add(probe);
            FactoryProbe again = pool.Get();
            TestHarness.Require(again.Value == 0, "Register(null) 不应清掉已有回调");
        }

        private static void Failure_RegisterDuplicate_KeepsFirstCallback()
        {
            PoolCallbackFactory.Instance.Register<DupProbe>(p => p.Value = 1);
            PoolCallbackFactory.Instance.Register<DupProbe>(p => p.Value = 999);
            Pool<DupProbe> pool = new Pool<DupProbe>(() => new DupProbe(), null);
            DupProbe probe = pool.Get();
            probe.Value = 0;
            pool.Add(probe);
            DupProbe again = pool.Get();
            TestHarness.Require(again.Value == 1, $"重复注册应保留第一次回调，实际 {again.Value}");
        }

        private static void Stress_PoolGetAdd256_CountMatches()
        {
            Pool<Probe> pool = NewExplicitPool();
            Probe[] items = new Probe[256];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = pool.Get();
            }

            TestHarness.Require(pool.Count == 0, "全部借出后 Count 应为 0");
            for (int i = 0; i < items.Length; i++)
            {
                pool.Add(items[i]);
            }

            TestHarness.Require(pool.Count == 256, $"256 次归还后 Count 应为 256，实际 {pool.Count}");
        }

        private static void Stress_ListPoolGetRelease64_AlwaysEmpty()
        {
            try
            {
                for (int i = 0; i < 64; i++)
                {
                    List<int> list = ListPool<int>.Get();
                    list.Add(i);
                    ListPool<int>.Release(list);
                    List<int> reused = ListPool<int>.Get();
                    TestHarness.Require(reused.Count == 0, $"第 {i} 次复用应为空，实际 {reused.Count}");
                    ListPool<int>.Release(reused);
                }
            }
            finally
            {
                ListPool<int>.Instance.Clear();
            }
        }

        private static void Contract_DictionaryPool_ReleaseClears()
        {
            Dictionary<int, string> dictionary = DictionaryPool<int, string>.Get();
            dictionary[1] = "a";
            DictionaryPool<int, string>.Release(dictionary);
            Dictionary<int, string> reused = DictionaryPool<int, string>.Get();
            try
            {
                TestHarness.Require(reused.Count == 0, $"Dictionary 归还应 Clear，实际 {reused.Count}");
            }
            finally
            {
                DictionaryPool<int, string>.Release(reused);
                DictionaryPool<int, string>.Instance.Clear();
            }
        }

        private static void Contract_HashSetPool_ReleaseClears()
        {
            HashSet<int> hashSet = HashSetPool<int>.Get();
            hashSet.Add(3);
            HashSetPool<int>.Release(hashSet);
            HashSet<int> reused = HashSetPool<int>.Get();
            try
            {
                TestHarness.Require(reused.Count == 0, $"HashSet 归还应 Clear，实际 {reused.Count}");
            }
            finally
            {
                HashSetPool<int>.Release(reused);
                HashSetPool<int>.Instance.Clear();
            }
        }

        private static void Contract_LinkedListPool_ReleaseClears()
        {
            LinkedList<int> linked = LinkedListPool<int>.Get();
            linked.AddLast(1);
            linked.AddLast(2);
            LinkedListPool<int>.Release(linked);
            LinkedList<int> reused = LinkedListPool<int>.Get();
            try
            {
                TestHarness.Require(reused.Count == 0, $"LinkedList 归还应 Clear，实际 {reused.Count}");
            }
            finally
            {
                LinkedListPool<int>.Release(reused);
                LinkedListPool<int>.Instance.Clear();
            }
        }

        private static void Contract_QueuePool_ReleaseClears()
        {
            Queue<int> queue = QueuePool<int>.Get();
            queue.Enqueue(1);
            queue.Enqueue(2);
            QueuePool<int>.Release(queue);
            Queue<int> reused = QueuePool<int>.Get();
            try
            {
                TestHarness.Require(reused.Count == 0, $"Queue 归还应 Clear，实际 {reused.Count}");
                reused.Enqueue(9);
                TestHarness.Require(reused.Dequeue() == 9, "清空后应仍是队列语义");
            }
            finally
            {
                QueuePool<int>.Release(reused);
                QueuePool<int>.Instance.Clear();
            }
        }

        private static void Contract_StackPool_ReleaseClears()
        {
            Stack<int> stack = StackPool<int>.Get();
            stack.Push(1);
            stack.Push(2);
            StackPool<int>.Release(stack);
            Stack<int> reused = StackPool<int>.Get();
            try
            {
                TestHarness.Require(reused.Count == 0, $"Stack 归还应 Clear，实际 {reused.Count}");
                reused.Push(8);
                TestHarness.Require(reused.Pop() == 8, "清空后应仍是栈语义");
            }
            finally
            {
                StackPool<int>.Release(reused);
                StackPool<int>.Instance.Clear();
            }
        }

        private static void Contract_StringBuilderPool_ReleaseClears()
        {
            StringBuilder builder = StringBuilderPool.Get();
            builder.Append("abc");
            StringBuilderPool.Release(builder);
            StringBuilder reused = StringBuilderPool.Get();
            try
            {
                TestHarness.Require(reused.Length == 0, $"StringBuilder 归还应 Clear，实际 Length={reused.Length}");
            }
            finally
            {
                StringBuilderPool.Release(reused);
                StringBuilderPool.Instance.Clear();
            }
        }

        private static void Contract_TwoArgPool_UsesFactoryReset()
        {
            PoolCallbackFactory.Instance.Register<FactoryProbe>(p => p.Value = 0);
            Pool<FactoryProbe> pool = new Pool<FactoryProbe>(() => new FactoryProbe(), null);
            FactoryProbe probe = pool.Get();
            probe.Value = 6;
            pool.Add(probe);
            FactoryProbe again = pool.Get();
            TestHarness.Require(again.Value == 0, $"两参构造应从工厂取置空，实际 {again.Value}");
        }

        private static void Regression_ListPoolReuse_DoesNotKeepOldItems()
        {
            List<int> first = ListPool<int>.Get();
            first.Add(42);
            ListPool<int>.Release(first);
            List<int> second = ListPool<int>.Get();
            try
            {
                TestHarness.Require(!second.Contains(42), "复用的 List 不应残留旧元素");
            }
            finally
            {
                ListPool<int>.Release(second);
                ListPool<int>.Instance.Clear();
            }
        }

        private static void Integration_PoolManager_RegisterGetRemoveClear()
        {
            int destroyed = 0;
            PoolManager manager = new PoolManager();
            Pool<Probe> pool = new Pool<Probe>(() => new Probe(), _ => destroyed++, p => p.Value = 0);
            manager.Register(pool);
            IPool<Probe> found = manager.Get<Probe>();
            TestHarness.Require(found == pool, "Get 应返回注册的池");
            Probe probe = found.Get();
            found.Add(probe);
            TestHarness.Require(found.Count == 1, "经管理器借还应作用在同一池上");
            manager.Clear();
            TestHarness.Require(pool.Count == 0, "Manager.Clear 应清空已注册池");
            TestHarness.Require(destroyed == 1, $"Manager.Clear 应触发 destroy，实际 {destroyed}");
            TestHarness.Require(manager.Get<Probe>() == null, "Clear 后类型表应空");
        }

        private static void Integration_ListActionInvoke_SurvivesHashSetPool()
        {
            ListAction<int> listAction = new ListAction<int>();
            int n = 0;
            listAction.Subscribe(v => n += v);
            listAction.Invoke(2);
            listAction.Invoke(3);
            TestHarness.Require(n == 5, $"ListAction 两次 Invoke 应成功（内部用 HashSetPool），实际 {n}");
        }
    }
}
