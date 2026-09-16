using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZFrameWork.PlayTests
{
    /// <summary>响应式数据模块 Play 模式测试套件（ReactiveValue / ValueComparer / ReactiveList / ReactiveDictionary / ReactiveSubscription）。</summary>
    public static class ReactiveDataTests
    {
        public static async Task RunAll()
        {
            TestHarness.Reset();
            try
            {
                // ---- ReactiveValue：冒烟 / 基本契约 ----
                await TestHarness.RunCaseAsync("Subscribe_DeliversCurrentImmediately",
                    Subscribe_DeliversCurrentImmediately);
                await TestHarness.RunCaseAsync("SetValue_ChangedValue_NotifiesAndReturnsTrue",
                    SetValue_ChangedValue_NotifiesAndReturnsTrue);
                await TestHarness.RunCaseAsync("ValueProperty_RoutesThroughSetValue",
                    ValueProperty_RoutesThroughSetValue);

                // ---- 值相等短路 ----
                await TestHarness.RunCaseAsync("SetValue_SameInt_NoNotifyReturnsFalse",
                    SetValue_SameInt_NoNotifyReturnsFalse);
                await TestHarness.RunCaseAsync("SetValue_SameString_NoNotify",
                    SetValue_SameString_NoNotify);
                await TestHarness.RunCaseAsync("SetValue_SameVector3_ExactComparison_NoNotify",
                    SetValue_SameVector3_ExactComparison_NoNotify);
                await TestHarness.RunCaseAsync("SetValue_FloatNaN_TreatedEqual_NoNotify",
                    SetValue_FloatNaN_TreatedEqual_NoNotify);
                await TestHarness.RunCaseAsync("SetValue_ReferenceType_NewInstance_Notifies",
                    SetValue_ReferenceType_NewInstance_Notifies);
                await TestHarness.RunCaseAsync("ValueComparer_UnityObject_PseudoNull_TreatedDistinct",
                    ValueComparer_UnityObject_PseudoNull_TreatedDistinct);

                // ---- 生命周期 / 订阅管理 ----
                await TestHarness.RunCaseAsync("UnsubscribeHandle_StopsDelivery_Idempotent",
                    UnsubscribeHandle_StopsDelivery_Idempotent);
                await TestHarness.RunCaseAsync("UnsubscribeByCallback_MethodGroup_StopsDelivery",
                    UnsubscribeByCallback_MethodGroup_StopsDelivery);
                await TestHarness.RunCaseAsync("DuplicateSubscribe_Deduplicated",
                    DuplicateSubscribe_Deduplicated);
                await TestHarness.RunCaseAsync("ClearSubscribers_NobodyNotified",
                    ClearSubscribers_NobodyNotified);
                await TestHarness.RunCaseAsync("SubscribeNull_ReturnsNoOpHandle_Invokable",
                    SubscribeNull_ReturnsNoOpHandle_Invokable);
                await TestHarness.RunCaseAsync("FirstCallbackThrows_SubscriptionNotEstablished",
                    FirstCallbackThrows_SubscriptionNotEstablished);
                await TestHarness.RunCaseAsync("UnsubscribeMissingCallback_ReturnsFalse",
                    UnsubscribeMissingCallback_ReturnsFalse);

                // ---- 变更 / 重入 ----
                await TestHarness.RunCaseAsync("Broadcast_UnsubscribeOther_OtherSkipsCurrent",
                    Broadcast_UnsubscribeOther_OtherSkipsCurrent);
                await TestHarness.RunCaseAsync("Broadcast_SetValueSameInCallback_NoInfiniteLoop",
                    Broadcast_SetValueSameInCallback_NoInfiniteLoop);
                await TestHarness.RunCaseAsync("Broadcast_SubscriberThrows_Isolated_OthersStillNotified",
                    Broadcast_SubscriberThrows_Isolated_OthersStillNotified);

                // ---- ReactiveList ----
                await TestHarness.RunCaseAsync("List_Subscribe_DeliversCurrentItems",
                    List_Subscribe_DeliversCurrentItems);
                await TestHarness.RunCaseAsync("List_AddRemove_NotifiesCount",
                    List_AddRemove_NotifiesCount);
                await TestHarness.RunCaseAsync("List_RemoveMissing_ReturnsFalseNoNotify",
                    List_RemoveMissing_ReturnsFalseNoNotify);
                await TestHarness.RunCaseAsync("List_RemoveAtOutOfRange_LoggedNoThrow",
                    List_RemoveAtOutOfRange_LoggedNoThrow);
                await TestHarness.RunCaseAsync("List_GetValueOutOfRange_ReturnsFalse",
                    List_GetValueOutOfRange_ReturnsFalse);
                await TestHarness.RunCaseAsync("List_SetValueSameValue_ShortCircuits",
                    List_SetValueSameValue_ShortCircuits);
                await TestHarness.RunCaseAsync("List_SetValueOutOfRange_ReturnsFalse",
                    List_SetValueOutOfRange_ReturnsFalse);
                await TestHarness.RunCaseAsync("List_ClearEmpty_NoNotify",
                    List_ClearEmpty_NoNotify);
                await TestHarness.RunCaseAsync("List_AddDuringBroadcast_ReentrantSafe",
                    List_AddDuringBroadcast_ReentrantSafe);

                // ---- ReactiveDictionary ----
                await TestHarness.RunCaseAsync("Dict_Subscribe_DeliversCurrentItems",
                    Dict_Subscribe_DeliversCurrentItems);
                await TestHarness.RunCaseAsync("Dict_AddDuplicateKey_FailsNoNotify",
                    Dict_AddDuplicateKey_FailsNoNotify);
                await TestHarness.RunCaseAsync("Dict_SetValueMissingKey_Fails",
                    Dict_SetValueMissingKey_Fails);
                await TestHarness.RunCaseAsync("Dict_SetValueSameValue_ShortCircuits",
                    Dict_SetValueSameValue_ShortCircuits);
                await TestHarness.RunCaseAsync("Dict_RemoveAndClear_Semantics",
                    Dict_RemoveAndClear_Semantics);
                await TestHarness.RunCaseAsync("Dict_TryGetValueAndContainsKey",
                    Dict_TryGetValueAndContainsKey);

                // ---- 压力 / 重复 ----
                await TestHarness.RunCaseAsync("Stress_ManySubscribers_HighFrequencySetValue",
                    Stress_ManySubscribers_HighFrequencySetValue);
                await TestHarness.RunCaseAsync("Stress_ListHeavyMutation_EndsConsistent",
                    Stress_ListHeavyMutation_EndsConsistent);

                // ---- 回归：暂无历史缺陷记录 ----
                TestHarness.Skip("Regression_NoRecordedBugs",
                    "尚无历史缺陷用例；发现缺陷后在此保留具名回归测试");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[TEST][SUITE] 响应式数据：Play 模式已退出，剩余用例中止。");
                return;
            }

            TestHarness.Summary("ReactiveData");
        }

        // ---------- ReactiveValue：基本契约 ----------

        private static Task Subscribe_DeliversCurrentImmediately()
        {
            var value = new ReactiveValue<int>(42);
            int received = 0;
            int calls = 0;
            value.Subscribe(v => { received = v; calls++; });

            TestHarness.Require(calls == 1, $"订阅应立即收到当前值一次，实际 {calls} 次");
            TestHarness.Require(received == 42, $"应收到 42，实际 {received}");
            return Task.CompletedTask;
        }

        private static Task SetValue_ChangedValue_NotifiesAndReturnsTrue()
        {
            var value = new ReactiveValue<int>(1);
            int received = 0;
            value.Subscribe(v => received = v);

            bool accepted = value.SetValue(5);
            TestHarness.Require(accepted, "值变化时 SetValue 应返回 true");
            TestHarness.Require(received == 5, $"应通知新值 5，实际 {received}");
            return Task.CompletedTask;
        }

        private static Task ValueProperty_RoutesThroughSetValue()
        {
            var value = new ReactiveValue<int>(1);
            int received = 0;
            value.Subscribe(v => received = v);

            value.Value = 9;
            TestHarness.Require(value.Value == 9, "属性读应返回当前值");
            TestHarness.Require(received == 9, "属性写应走 SetValue 并通知");

            value.Value = 9;   // 同值短路
            TestHarness.Require(received == 9, "同值赋值不应再次通知");
            return Task.CompletedTask;
        }

        // ---------- 值相等短路 ----------

        private static Task SetValue_SameInt_NoNotifyReturnsFalse()
        {
            var value = new ReactiveValue<int>(7);
            int calls = 0;
            value.Subscribe(_ => calls++);

            bool accepted = value.SetValue(7);
            TestHarness.Require(!accepted, "同值 SetValue 应返回 false");
            TestHarness.Require(calls == 1, $"同值不应通知（仅订阅时 1 次），实际 {calls} 次");
            return Task.CompletedTask;
        }

        private static Task SetValue_SameString_NoNotify()
        {
            var value = new ReactiveValue<string>("abc");
            int calls = 0;
            value.Subscribe(_ => calls++);

            // 内容相同但引用不同——string 走 EqualityComparer<string>.Default 内容比较
            value.SetValue(new string('a', 1) + "bc");
            TestHarness.Require(calls == 1, $"内容相同的字符串不应通知，实际 {calls} 次");
            return Task.CompletedTask;
        }

        private static Task SetValue_SameVector3_ExactComparison_NoNotify()
        {
            var value = new ReactiveValue<Vector3>(new Vector3(1f, 2f, 3f));
            int calls = 0;
            value.Subscribe(_ => calls++);

            value.SetValue(new Vector3(1f, 2f, 3f));
            TestHarness.Require(calls == 1, $"相同 Vector3（精确比较）不应通知，实际 {calls} 次");

            value.SetValue(new Vector3(1f, 2f, 3.5f));   // 差异需大于 float ULP，过小的字面量会舍入回原值
            TestHarness.Require(calls == 2, $"微小差异应视为不同并通知，实际 {calls} 次");
            return Task.CompletedTask;
        }

        private static Task SetValue_FloatNaN_TreatedEqual_NoNotify()
        {
            var value = new ReactiveValue<float>(float.NaN);
            int calls = 0;
            value.Subscribe(_ => calls++);

            value.SetValue(float.NaN);
            TestHarness.Require(calls == 1,
                $"记录契约：EqualityComparer<float>.Default 判 NaN 相等，同 NaN 不通知，实际 {calls} 次");
            return Task.CompletedTask;
        }

        private static Task SetValue_ReferenceType_NewInstance_Notifies()
        {
            // 引用类型走默认引用比较：整体替换语义——换实例即通知
            var first = new object();
            var second = new object();
            var value = new ReactiveValue<object>(first);
            int calls = 0;
            value.Subscribe(_ => calls++);

            value.SetValue(second);
            TestHarness.Require(calls == 2, $"引用类型换实例应通知，实际 {calls} 次");
            return Task.CompletedTask;
        }

        private static Task ValueComparer_UnityObject_PseudoNull_TreatedDistinct()
        {
            // 记录现状契约：ValueComparer 未特判 UnityEngine.Object，
            // EqualityComparer<GameObject>.Default 把"已销毁"与 null 判为不同 → 会通知
            GameObject obj = new GameObject("ReactiveTestTarget");
            var value = new ReactiveValue<GameObject>(obj);
            int calls = 0;
            value.Subscribe(_ => calls++);

            Object.DestroyImmediate(obj);
            TestHarness.Require(obj == null, "前置：DestroyImmediate 后对象应为伪 null");

            bool comparerSaysEqual = ValueComparer<GameObject>.Compare(obj, null);
            TestHarness.Require(!comparerSaysEqual,
                "记录现状：默认比较器将已销毁对象与 null 判为不同（未做伪 null 特判）");

            value.SetValue(null);
            TestHarness.Require(calls == 2, "按上述契约，销毁对象 → null 应触发通知");
            return Task.CompletedTask;
        }

        // ---------- 生命周期 / 订阅管理 ----------

        private static Task UnsubscribeHandle_StopsDelivery_Idempotent()
        {
            var value = new ReactiveValue<int>(0);
            int calls = 0;
            Action unsubscribe = value.Subscribe(_ => calls++);

            value.SetValue(1);
            int callsAfterFirst = calls;

            unsubscribe();
            unsubscribe();   // 幂等：第二次应无副作用
            unsubscribe();

            value.SetValue(2);
            TestHarness.Require(callsAfterFirst == 2, $"退订前应收到订阅+变更两次，实际 {callsAfterFirst}");
            TestHarness.Require(calls == 2, $"退订后不应再收到通知，实际 {calls} 次");
            return Task.CompletedTask;
        }

        private sealed class Receiver
        {
            public int Calls;
            public void OnChanged(int v) => Calls++;
        }

        private static Task UnsubscribeByCallback_MethodGroup_StopsDelivery()
        {
            var value = new ReactiveValue<int>(0);
            var receiver = new Receiver();
            value.Subscribe(receiver.OnChanged);

            value.SetValue(1);
            TestHarness.Require(receiver.Calls == 2, "方法组订阅应收到通知");

            bool removed = value.Unsubscribe(receiver.OnChanged);
            TestHarness.Require(removed, "方法组退订应成功（委托相等：同 Target + 同 Method）");

            value.SetValue(2);
            TestHarness.Require(receiver.Calls == 2, $"退订后不应再收到，实际 {receiver.Calls} 次");
            return Task.CompletedTask;
        }

        private static Task DuplicateSubscribe_Deduplicated()
        {
            var value = new ReactiveValue<int>(0);
            var receiver = new Receiver();
            value.Subscribe(receiver.OnChanged);
            int afterFirstSubscribe = receiver.Calls;   // 订阅即推：立即投递 1 次

            value.Subscribe(receiver.OnChanged);   // 相同委托，登记被去重，但订阅即推仍会立即投递 1 次
            TestHarness.Require(receiver.Calls == afterFirstSubscribe + 1,
                "第二次订阅虽被去重，订阅即推语义仍应投递一次当前值");

            value.SetValue(1);
            TestHarness.Require(receiver.Calls == afterFirstSubscribe + 2,
                $"去重生效：变更时只通知一次，实际 {receiver.Calls - afterFirstSubscribe - 1} 次");
            return Task.CompletedTask;
        }

        private static Task ClearSubscribers_NobodyNotified()
        {
            var value = new ReactiveValue<int>(0);
            var receiver = new Receiver();
            value.Subscribe(receiver.OnChanged);
            value.ClearSubscribers();

            value.SetValue(1);
            TestHarness.Require(receiver.Calls == 1, $"清除订阅后不应收到变更通知，实际 {receiver.Calls} 次");
            return Task.CompletedTask;
        }

        private static Task SubscribeNull_ReturnsNoOpHandle_Invokable()
        {
            var value = new ReactiveValue<int>(0);
            Action unsubscribe = value.Subscribe(null);   // 预期触发 ZLog 报错（契约内诊断）

            TestHarness.Require(unsubscribe != null, "空订阅应返回 no-op 句柄而非 null");
            unsubscribe();   // 应可安全调用
            unsubscribe();
            return Task.CompletedTask;
        }

        private static Task FirstCallbackThrows_SubscriptionNotEstablished()
        {
            var value = new ReactiveValue<int>(0);
            int establishedCalls = 0;
            Action unsubscribe = value.Subscribe(v => throw new InvalidOperationException("intended"));

            // 首次回调抛异常：订阅不应建立，句柄为 no-op
            unsubscribe();
            value.Subscribe(_ => establishedCalls++);
            value.SetValue(5);

            TestHarness.Require(establishedCalls == 2,
                $"异常订阅者不应存在；正常订阅者应收到订阅+变更两次，实际 {establishedCalls} 次");
            return Task.CompletedTask;
        }

        private static Task UnsubscribeMissingCallback_ReturnsFalse()
        {
            var value = new ReactiveValue<int>(0);
            var receiver = new Receiver();
            bool removed = value.Unsubscribe(receiver.OnChanged);
            TestHarness.Require(!removed, "退订从未订阅的回调应返回 false");
            return Task.CompletedTask;
        }

        // ---------- 变更 / 重入 ----------

        private static Task Broadcast_UnsubscribeOther_OtherSkipsCurrent()
        {
            var value = new ReactiveValue<int>(0);
            int bCalls = 0;

            Action<int> a = _ => value.Unsubscribe(BHandler);
            void BHandler(int _) => bCalls++;

            value.Subscribe(a);
            value.Subscribe(BHandler);

            bCalls = 0;
            value.SetValue(1);
            TestHarness.Require(bCalls == 0,
                $"A 在广播中退订 B 后，B 不应收到本次通知，实际 {bCalls} 次");

            value.SetValue(2);
            TestHarness.Require(bCalls == 0, $"后续通知 B 也不应再收到，实际 {bCalls} 次");
            return Task.CompletedTask;
        }

        private static Task Broadcast_SetValueSameInCallback_NoInfiniteLoop()
        {
            var value = new ReactiveValue<int>(1);
            int calls = 0;
            value.Subscribe(v =>
            {
                calls++;
                // 广播中写回当前正在传播的值 → 与 _value 相同，短路，不产生嵌套广播
                value.SetValue(v);
            });

            value.SetValue(2);
            TestHarness.Require(calls == 2, $"回调写回同值应短路（订阅 1 + 变更 1），实际 {calls} 次");
            TestHarness.Require(value.Value == 2, "最终值应为 2");
            return Task.CompletedTask;
        }

        private static Task Broadcast_SubscriberThrows_Isolated_OthersStillNotified()
        {
            var value = new ReactiveValue<int>(0);
            int throwingCalls = 0;
            // 首次回调（订阅即推）必须成功才能建立订阅；从第二次起抛异常
            value.Subscribe(v =>
            {
                throwingCalls++;
                if (throwingCalls > 1) throw new InvalidOperationException("intended-later");
            });

            int otherCalls = 0;
            value.Subscribe(_ => otherCalls++);

            Exception caught = null;
            try { value.SetValue(5); }
            catch (Exception e) { caught = e; }

            // ListAction.Invoke 已内建异常隔离：记日志、不传播、后续订阅者继续
            TestHarness.Require(caught == null,
                $"订阅者异常应被隔离，不应传播到 SetValue 调用方，实际抛出 {caught?.GetType().Name}");
            TestHarness.Require(throwingCalls == 2, "抛异常的订阅者应被调用过（订阅 + 变更）");
            TestHarness.Require(otherCalls == 2,
                $"异常之后的订阅者仍应收到本次通知（隔离生效），实际 {otherCalls} 次");
            TestHarness.Require(value.Value == 5, "值应已更新（先赋值后广播）");
            return Task.CompletedTask;
        }

        // ---------- ReactiveList ----------

        private static Task List_Subscribe_DeliversCurrentItems()
        {
            var list = new ReactiveList<int>(new[] { 1, 2, 3 });
            IReadOnlyList<int> received = null;
            int calls = 0;
            list.Subscribe(items => { received = items; calls++; });

            TestHarness.Require(calls == 1, "订阅应立即收到当前列表");
            TestHarness.Require(received != null && received.Count == 3, "订阅时应收到 3 个元素");
            return Task.CompletedTask;
        }

        private static Task List_AddRemove_NotifiesCount()
        {
            var list = new ReactiveList<string>();
            int count = -1;
            int calls = 0;
            list.Subscribe(items => { count = items.Count; calls++; });

            list.Add("a");
            list.Add("b");
            bool removed = list.Remove("a");

            TestHarness.Require(removed, "Remove 存在元素应返回 true");
            TestHarness.Require(count == 1, $"最终通知的 Count 应为 1，实际 {count}");
            TestHarness.Require(calls == 4, $"订阅 1 次 + Add×2 + Remove×1 = 4 次回调，实际 {calls} 次");
            return Task.CompletedTask;
        }

        private static Task List_RemoveMissing_ReturnsFalseNoNotify()
        {
            var list = new ReactiveList<int>();
            int calls = 0;
            list.Subscribe(_ => calls++);

            bool removed = list.Remove(99);
            TestHarness.Require(!removed, "移除不存在的元素应返回 false");
            TestHarness.Require(calls == 1, $"不应通知，实际 {calls} 次");
            return Task.CompletedTask;
        }

        private static Task List_RemoveAtOutOfRange_LoggedNoThrow()
        {
            var list = new ReactiveList<int>(new[] { 1 });
            int calls = 0;
            list.Subscribe(_ => calls++);

            list.RemoveAt(5);    // 预期 ZLog 报错（契约内诊断），不应抛异常
            list.RemoveAt(-1);

            TestHarness.Require(list.Count == 1, "越界删除不应改变列表");
            TestHarness.Require(calls == 1, $"越界删除不应通知，实际 {calls} 次");
            return Task.CompletedTask;
        }

        private static Task List_GetValueOutOfRange_ReturnsFalse()
        {
            var list = new ReactiveList<int>(new[] { 1, 2 });
            bool ok = list.GetValue(2, out int value);

            TestHarness.Require(!ok, "越界读取应返回 false");
            TestHarness.Require(value == 0, "越界读取 out 参数应为默认值");

            TestHarness.Require(list.GetValue(1, out int last) && last == 2, "正常读取应返回值");
            return Task.CompletedTask;
        }

        private static Task List_SetValueSameValue_ShortCircuits()
        {
            var list = new ReactiveList<int>(new[] { 1, 2 });
            int calls = 0;
            list.Subscribe(_ => calls++);

            bool changed = list.SetValue(0, 1);
            TestHarness.Require(!changed, "同值替换应返回 false");
            TestHarness.Require(calls == 1, $"同值替换不应通知，实际 {calls} 次");

            TestHarness.Require(list.SetValue(0, 9), "换值替换应返回 true");
            TestHarness.Require(calls == 2, "换值应通知");
            return Task.CompletedTask;
        }

        private static Task List_SetValueOutOfRange_ReturnsFalse()
        {
            var list = new ReactiveList<int>(new[] { 1 });
            int calls = 0;
            list.Subscribe(_ => calls++);

            bool changed = list.SetValue(3, 0);   // 预期 ZLog 报错
            TestHarness.Require(!changed, "越界修改应返回 false");
            TestHarness.Require(calls == 1, $"越界修改不应通知，实际 {calls} 次");
            return Task.CompletedTask;
        }

        private static Task List_ClearEmpty_NoNotify()
        {
            var list = new ReactiveList<int>();
            int calls = 0;
            list.Subscribe(_ => calls++);

            list.Clear();
            TestHarness.Require(calls == 1, $"清空空列表不应通知，实际 {calls} 次");
            return Task.CompletedTask;
        }

        private static Task List_AddDuringBroadcast_ReentrantSafe()
        {
            var list = new ReactiveList<int>();
            int totalNotifications = 0;
            int reentrantAdds = 0;
            list.Subscribe(items =>
            {
                totalNotifications++;
                // 广播中再 Add：应触发重入广播，但只做一次（守卫防无限递归）
                if (reentrantAdds == 0 && items.Count == 1)
                {
                    reentrantAdds++;
                    list.Add(2);
                }
            });

            list.Add(1);
            TestHarness.Require(list.Count == 2, $"重入添加后应有 2 个元素，实际 {list.Count}");
            TestHarness.Require(totalNotifications >= 2, $"应至少有两次通知（外层+重入），实际 {totalNotifications}");
            return Task.CompletedTask;
        }

        // ---------- ReactiveDictionary ----------

        private static Task Dict_Subscribe_DeliversCurrentItems()
        {
            var dict = new ReactiveDictionary<string, int>();
            dict.Add("hp", 100);

            IReadOnlyDictionary<string, int> received = null;
            dict.Subscribe(items => received = items);

            TestHarness.Require(received != null && received.Count == 1 && received["hp"] == 100,
                "订阅时应收到当前字典内容");
            return Task.CompletedTask;
        }

        private static Task Dict_AddDuplicateKey_FailsNoNotify()
        {
            var dict = new ReactiveDictionary<string, int>();
            dict.Add("hp", 100);
            int calls = 0;
            dict.Subscribe(_ => calls++);

            bool added = dict.Add("hp", 50);   // 预期 ZLog 报错
            TestHarness.Require(!added, "重复键 Add 应返回 false");
            TestHarness.Require(dict.TryGetValue("hp", out int hp) && hp == 100, "原值不应被覆盖");
            TestHarness.Require(calls == 1, $"重复键 Add 不应通知，实际 {calls} 次");
            return Task.CompletedTask;
        }

        private static Task Dict_SetValueMissingKey_Fails()
        {
            var dict = new ReactiveDictionary<string, int>();
            int calls = 0;
            dict.Subscribe(_ => calls++);

            bool changed = dict.SetValue("missing", 1);   // 预期 ZLog 报错
            TestHarness.Require(!changed, "不存在的键 SetValue 应返回 false");
            TestHarness.Require(calls == 1, $"不应通知，实际 {calls} 次");
            return Task.CompletedTask;
        }

        private static Task Dict_SetValueSameValue_ShortCircuits()
        {
            var dict = new ReactiveDictionary<string, int>();
            dict.Add("hp", 100);
            int calls = 0;
            dict.Subscribe(_ => calls++);

            TestHarness.Require(!dict.SetValue("hp", 100), "同值修改应返回 false");
            TestHarness.Require(calls == 1, $"同值修改不应通知，实际 {calls} 次");

            TestHarness.Require(dict.SetValue("hp", 80), "换值修改应返回 true");
            TestHarness.Require(calls == 2, "换值应通知");
            return Task.CompletedTask;
        }

        private static Task Dict_RemoveAndClear_Semantics()
        {
            var dict = new ReactiveDictionary<string, int>();
            dict.Add("a", 1);
            dict.Add("b", 2);
            int count = -1;
            int calls = 0;
            dict.Subscribe(items => { count = items.Count; calls++; });

            TestHarness.Require(dict.Remove("a"), "移除存在的键应返回 true");
            TestHarness.Require(count == 1, $"移除后通知 Count 应为 1，实际 {count}");

            TestHarness.Require(!dict.Remove("zz"), "移除不存在的键应返回 false");

            dict.Clear();
            TestHarness.Require(count == 0 && calls == 3, "清空应通知且 Count 为 0");

            int beforeEmptyClear = calls;
            dict.Clear();
            TestHarness.Require(calls == beforeEmptyClear, "清空空字典不应通知");
            return Task.CompletedTask;
        }

        private static Task Dict_TryGetValueAndContainsKey()
        {
            var dict = new ReactiveDictionary<int, string>();
            dict.Add(1, "one");

            TestHarness.Require(dict.ContainsKey(1) && !dict.ContainsKey(2), "ContainsKey 语义");
            TestHarness.Require(dict.TryGetValue(1, out string v) && v == "one", "TryGetValue 命中");
            TestHarness.Require(!dict.TryGetValue(2, out _), "TryGetValue 未命中返回 false");
            return Task.CompletedTask;
        }

        // ---------- 压力 ----------

        private static Task Stress_ManySubscribers_HighFrequencySetValue()
        {
            const int subscriberCount = 100;
            const int setCount = 1000;
            var value = new ReactiveValue<int>(0);

            Receiver[] receivers = new Receiver[subscriberCount];
            Action[] handles = new Action[subscriberCount];
            for (int i = 0; i < subscriberCount; i++)
            {
                receivers[i] = new Receiver();
                Receiver receiver = receivers[i];   // 闭包捕获当前
                handles[i] = value.Subscribe(receiver.OnChanged);
            }

            for (int i = 1; i <= setCount; i++)
                value.SetValue(i);

            for (int i = 0; i < subscriberCount; i++)
            {
                TestHarness.Require(receivers[i].Calls == setCount + 1,
                    $"订阅者 {i} 应收到订阅+{setCount} 次变更，实际 {receivers[i].Calls}");
            }

            for (int i = 0; i < subscriberCount; i++)
                handles[i].DisposeAll();   // 见下方扩展
            value.SetValue(setCount + 1);
            for (int i = 0; i < subscriberCount; i++)
                TestHarness.Require(receivers[i].Calls == setCount + 1,
                    $"退订后订阅者 {i} 不应再收到");

            return Task.CompletedTask;
        }

        private static Task Stress_ListHeavyMutation_EndsConsistent()
        {
            const int rounds = 500;
            var list = new ReactiveList<int>();
            int lastSeenCount = 0;
            list.Subscribe(items => lastSeenCount = items.Count);

            for (int i = 0; i < rounds; i++)
                list.Add(i);
            TestHarness.Require(list.Count == rounds && lastSeenCount == rounds, "批量 Add 后计数应一致");

            for (int i = 0; i < rounds / 2; i++)
                TestHarness.Require(list.Remove(i), "移除已存在元素应成功");
            TestHarness.Require(list.Count == rounds / 2, $"剩余元素数应为 {rounds / 2}，实际 {list.Count}");

            list.Clear();
            TestHarness.Require(list.Count == 0 && lastSeenCount == 0, "清空后应为空");
            return Task.CompletedTask;
        }
    }

    internal static class ReactiveTestExtensions
    {
        /// <summary>语义化调用退订句柄（句柄本身是 Action）。</summary>
        public static void DisposeAll(this Action unsubscribe) => unsubscribe();
    }
}
