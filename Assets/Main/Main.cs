using System;
using System.Text;
using UnityEngine;
using ZFrameWork;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Main : MonoBehaviour
{
    private bool _hasRun;
    private int _total;
    private int _passed;
    private int _failed;
    private int _skipped = 0;
#if UNITY_EDITOR
    private static bool _batchPlayModeTestCompleted;
#endif

    private void Start()
    {
        if (_hasRun) return;
        _hasRun = true;

        RunSuite("ListAction.Basic", RunListActionTests);
        RunSuite("ListEvent.Basic", RunListEventTests);
        RunSuite("Delegate.EdgeCases", RunDelegateEdgeCaseTests);
        RunSuite("EventCenter.Integration", RunEventCenterTests);
        RunSuite("CollectionPool.Basic", RunCollectionPoolTests);
        ZLog.Log($"[TEST][SUMMARY] total={_total}, passed={_passed}, failed={_failed}, skipped={_skipped}");
#if UNITY_EDITOR
        _batchPlayModeTestCompleted = true;
#endif
    }

#if UNITY_EDITOR
    public static void RunBatchPlayModeTests()
    {
        _batchPlayModeTestCompleted = false;
        EditorApplication.update -= WaitForBatchPlayModeTests;
        EditorApplication.update += WaitForBatchPlayModeTests;
        EditorApplication.isPlaying = true;
    }

    private static void WaitForBatchPlayModeTests()
    {
        if (!Application.isPlaying || !_batchPlayModeTestCompleted)
        {
            return;
        }

        EditorApplication.update -= WaitForBatchPlayModeTests;
        EditorApplication.update += ExitAfterBatchPlayModeTests;
        EditorApplication.isPlaying = false;
    }

    private static void ExitAfterBatchPlayModeTests()
    {
        if (EditorApplication.isPlaying)
        {
            return;
        }

        EditorApplication.update -= ExitAfterBatchPlayModeTests;
        EditorApplication.Exit(0);
    }
#endif

    private void RunCollectionPoolTests()
    {
        RunCase("ListPool_GetRelease_ClearsContents", () =>
        {
            var list = ListPool<int>.Get();
            list.Add(1);
            list.Add(2);
            ListPool<int>.Release(list);

            var reused = ListPool<int>.Get();
            Require(reused.Count == 0, "List 归还后再次获取时应该为空");
            ListPool<int>.Release(reused);
        });

        RunCase("ListPool_ReleaseNull_ExpectedDiagnostic_DoesNotFail", () =>
        {
            ZLog.Log("[TEST][EXPECTED-DIAGNOSTIC] Pool.Add null 归还会记录错误，但不应导致测试失败");
            ListPool<int>.Release(null);
        });

        RunCase("DictionaryPool_GetRelease_ClearsEntries", () =>
        {
            var dictionary = DictionaryPool<int, string>.Get();
            dictionary.Add(1, "one");
            dictionary.Add(2, "two");
            DictionaryPool<int, string>.Release(dictionary);

            var reused = DictionaryPool<int, string>.Get();
            Require(reused.Count == 0, "Dictionary 归还后再次获取时应该为空");
            DictionaryPool<int, string>.Release(reused);
        });

        RunCase("HashSetPool_GetRelease_ClearsEntries", () =>
        {
            var hashSet = HashSetPool<int>.Get();
            hashSet.Add(1);
            hashSet.Add(2);
            HashSetPool<int>.Release(hashSet);

            var reused = HashSetPool<int>.Get();
            Require(reused.Count == 0, "HashSet 归还后再次获取时应该为空");
            HashSetPool<int>.Release(reused);
        });

        RunCase("QueuePool_GetRelease_ClearsContentsAndPreservesQueueBehavior", () =>
        {
            var queue = QueuePool<int>.Get();
            queue.Enqueue(1);
            queue.Enqueue(2);
            QueuePool<int>.Release(queue);

            var reused = QueuePool<int>.Get();
            Require(reused.Count == 0, "Queue 归还后再次获取时应该为空");
            reused.Enqueue(1);
            reused.Enqueue(2);
            Require(reused.Dequeue() == 1 && reused.Dequeue() == 2,
                "Queue 应该保持先进先出行为");
            QueuePool<int>.Release(reused);
        });

        RunCase("StackPool_GetRelease_ClearsContentsAndPreservesStackBehavior", () =>
        {
            var stack = StackPool<int>.Get();
            stack.Push(1);
            stack.Push(2);
            StackPool<int>.Release(stack);

            var reused = StackPool<int>.Get();
            Require(reused.Count == 0, "Stack 归还后再次获取时应该为空");
            reused.Push(1);
            reused.Push(2);
            Require(reused.Pop() == 2 && reused.Pop() == 1,
                "Stack 应该保持后进先出行为");
            StackPool<int>.Release(reused);
        });

        RunCase("StringBuilderPool_GetRelease_ClearsContents", () =>
        {
            var builder = StringBuilderPool.Get();
            builder.Append("pooled text");
            StringBuilderPool.Release(builder);

            var reused = StringBuilderPool.Get();
            Require(reused.Length == 0, "StringBuilder 归还后再次获取时应该为空");
            reused.Append("next");
            Require(reused.ToString() == "next", "StringBuilder 应该可以继续追加内容");
            StringBuilderPool.Release(reused);
        });

        RunCase("LinkedListPool_GetRelease_ClearsNodesAndPreservesOrder", () =>
        {
            var linkedList = LinkedListPool<int>.Get();
            linkedList.AddLast(1);
            linkedList.AddLast(2);
            linkedList.AddFirst(0);
            LinkedListPool<int>.Release(linkedList);

            var reused = LinkedListPool<int>.Get();
            Require(reused.Count == 0, "LinkedList 归还后再次获取时应该为空");
            reused.AddLast(1);
            reused.AddLast(2);
            reused.AddLast(3);
            Require(reused.First.Value == 1 && reused.Last.Value == 3,
                "LinkedList 应该保持首尾节点顺序");
            LinkedListPool<int>.Release(reused);
        });

        RunCase("CollectionPool_MultipleGets_ReturnIndependentInstances", () =>
        {
            var first = ListPool<CollectionPoolItem>.Get();
            var second = ListPool<CollectionPoolItem>.Get();
            Require(!ReferenceEquals(first, second), "连续获取的集合不能是同一个实例");
            ListPool<CollectionPoolItem>.Release(first);
            ListPool<CollectionPoolItem>.Release(second);
        });

        RunCase("CollectionPool_DuplicateRelease_ExpectedDiagnostic_DoesNotDuplicatePooledInstance", () =>
        {
            var list = ListPool<CollectionPoolItem>.Get();
            ListPool<CollectionPoolItem>.Release(list);
            ZLog.Log("[TEST][EXPECTED-DIAGNOSTIC] Pool.Add 重复归还会记录错误，但不应导致测试失败");
            ListPool<CollectionPoolItem>.Release(list);

            var reused = ListPool<CollectionPoolItem>.Get();
            Require(ReferenceEquals(list, reused), "重复归还不能在池中生成重复实例");
            ListPool<CollectionPoolItem>.Release(reused);
        });

        RunCase("CollectionPool_ReferenceContents_AreClearedOnRelease", () =>
        {
            var list = ListPool<CollectionPoolItem>.Get();
            list.Add(new CollectionPoolItem { Value = 42 });
            ListPool<CollectionPoolItem>.Release(list);

            var reused = ListPool<CollectionPoolItem>.Get();
            Require(reused.Count == 0, "归还集合时应该清理其中的对象引用");
            ListPool<CollectionPoolItem>.Release(reused);
        });

        RunCase("CollectionPool_RepeatedReuse_RemainsEmptyAfterRelease", () =>
        {
            const int iterations = 1000;

            for (var i = 0; i < iterations; i++)
            {
                var list = ListPool<int>.Get();
                Require(list.Count == 0, "重复获取时集合不能残留上一次的数据");
                list.Add(i);
                ListPool<int>.Release(list);
            }
        });
    }

    private void RunListActionTests()
    {
        var action = new ListAction();
        int count = 0;
        Action callback = () => count++;

        action.Subscribe(callback);
        action.Subscribe(callback);
        action.Invoke();
        Check(count == 1 && action.Count == 1, "无参 LinkedAction 防止重复订阅");

        Check(action.Unsubscribe(callback), "无参 LinkedAction 取消订阅");
        action.Invoke();
        Check(count == 1 && action.Count == 0, "取消后不再触发");

        var typedAction = new ListAction<int>();
        int sum = 0;
        Action<int> first = value => sum += value;
        Action<int> second = value => sum += value * 10;

        typedAction.Subscribe(first);
        typedAction.Subscribe(second);
        typedAction.Invoke(2);
        Check(sum == 22 && typedAction.Count == 2, "有参 LinkedAction 传递参数");

        typedAction.Unsubscribe(first);
        typedAction.Invoke(2);
        Check(sum == 42 && typedAction.Count == 1, "有参 LinkedAction 移除指定回调");

        var mutationAction = new ListAction<int>();
        int selfCount = 0;
        Action<int> selfRemove = null;
        selfRemove = _ =>
        {
            selfCount++;
            mutationAction.Unsubscribe(selfRemove);
        };
        mutationAction.Subscribe(selfRemove);
        mutationAction.Subscribe(_ => selfCount += 10);
        mutationAction.Invoke(0);
        mutationAction.Invoke(0);
        Check(selfCount == 21, "回调执行过程中移除自身");
    }

    private void RunDelegateEdgeCaseTests()
    {
        RunCase("LinkedAction 委托相等性去重", () =>
        {
            var target = new DelegateTarget();
            var action = new ListAction();
            Action first = target.Handle;
            Action second = target.Handle;

            action.Subscribe(first);
            action.Subscribe(second);
            action.Invoke();

            Require(action.Count == 1, "同一实例的同一方法应该只订阅一次");
            Require(target.CallCount == 1, "去重后的委托应该只执行一次");
        });

        RunCase("LinkedAction 不同实例委托不去重", () =>
        {
            var firstTarget = new DelegateTarget();
            var secondTarget = new DelegateTarget();
            var action = new ListAction();

            action.Subscribe(firstTarget.Handle);
            action.Subscribe(secondTarget.Handle);
            action.Invoke();

            Require(action.Count == 2, "不同实例的方法应该分别订阅");
            Require(firstTarget.CallCount == 1 && secondTarget.CallCount == 1,
                "不同实例的委托应该分别执行");
        });

        RunCase("LinkedAction 保持订阅顺序", () =>
        {
            var action = new ListAction<int>();
            var order = ListPool<int>.Get();
            action.Subscribe(_ => order.Add(1));
            action.Subscribe(_ => order.Add(2));
            action.Subscribe(_ => order.Add(3));
            action.Invoke(0);

            Require(order.Count == 3, "所有回调都应该执行");
            Require(order[0] == 1 && order[1] == 2 && order[2] == 3,
                "回调执行顺序应该与订阅顺序一致");
            ListPool<int>.Release(order);
        });

        RunCase("LinkedAction 运算符订阅和取消", () =>
        {
            var action = new ListAction<int>();
            int callCount = 0;
            Action<int> callback = _ => callCount++;

            action += callback;
            action += callback;
            Require(action.Count == 1, "+ 运算符应该防止重复订阅");

            action.Invoke(0);
            action -= callback;
            action.Invoke(0);

            Require(callCount == 1 && action.Count == 0,
                "- 运算符应该移除回调");
        });

        RunCase("LinkedAction 回调中新增回调", () =>
        {
            var action = new ListAction();
            int firstCount = 0;
            int lateCount = 0;
            Action late = () => lateCount++;

            action.Subscribe(() =>
            {
                firstCount++;
                action.Subscribe(late);
            });
            action.Invoke();

            Require(firstCount == 1 && lateCount == 1,
                "回调中新增的回调应该按照既定语义执行");
        });

        RunCase("LinkedAction 回调中移除后续回调", () =>
        {
            var action = new ListAction();
            int removedCount = 0;
            Action removed = () => removedCount++;

            action.Subscribe(() => action.Unsubscribe(removed));
            action.Subscribe(removed);
            action.Invoke();

            Require(removedCount == 0 && action.Count == 1,
                "被移除的后续回调不应该执行");
        });

        RunCase("LinkedAction 回调中清空", () =>
        {
            var action = new ListAction();
            int laterCount = 0;

            action.Subscribe(action.Clear);
            action.Subscribe(() => laterCount++);
            action.Invoke();

            Require(action.Count == 0 && laterCount == 0,
                "清空后续回调不应该继续执行");
        });

        RunCase("LinkedAction 空值和不存在回调", () =>
        {
            var action = new ListAction();
            Action callback = () => { };

            action.Subscribe(null);
            Require(!action.Unsubscribe(null), "取消空回调应该返回 false");
            Require(!action.Unsubscribe(callback), "取消不存在回调应该返回 false");
            Require(action.Count == 0, "空回调不应该被加入");
        });

        RunCase("LinkedAction 批量订阅和移除", () =>
        {
            const int callbackCount = 1000;
            var action = new ListAction<int>();
            var callbacks = ListPool<Action<int>>.Get();
            var targets = ListPool<TypedDelegateTarget>.Get();
            for (var i = 0; i < callbackCount; i++)
            {
                var target = new TypedDelegateTarget();
                targets.Add(target);
                Action<int> callback = target.Handle;
                callbacks.Add(callback);
                action.Subscribe(callback);
            }

            Require(action.Count == callbackCount, "批量订阅数量不正确");
            action.Invoke(0);
            var invokedCount = 0;
            foreach (var target in targets)
            {
                invokedCount += target.CallCount;
            }
            Require(invokedCount == callbackCount, "批量订阅回调执行数量不正确");

            for (var i = 0; i < callbacks.Count; i += 2)
            {
                Require(action.Unsubscribe(callbacks[i]), "批量移除回调失败");
            }

            Require(action.Count == callbackCount / 2, "批量移除后的数量不正确");
            ListPool<Action<int>>.Release(callbacks);
            ListPool<TypedDelegateTarget>.Release(targets);
        });

        RunCase("LinkedEvent 事件语法和清空", () =>
        {
            var linkedEvent = new ListEvent();
            int callCount = 0;
            Action callback = () => callCount++;

            linkedEvent.Invoked += callback;
            linkedEvent.Invoked += callback;
            linkedEvent.Invoke();
            Require(callCount == 1 && linkedEvent.Count == 1,
                "事件语法应该支持去重");

            linkedEvent.Clear();
            linkedEvent.Invoke();
            Require(callCount == 1 && linkedEvent.Count == 0,
                "事件清空后不应该继续触发");
        });

        RunCase("LinkedAction_RemoveSameCallbackTwice_ReturnsFalseSecondTime", () =>
        {
            var action = new ListAction();
            Action callback = () => { };

            action.Subscribe(callback);
            Require(action.Unsubscribe(callback), "第一次取消订阅应该成功");
            Require(!action.Unsubscribe(callback), "第二次取消订阅应该返回 false");
            Require(action.Count == 0, "重复取消后数量应该为 0");
        });

        RunCase("LinkedAction_RemoveVisitedCallback_DoesNotReplayIt", () =>
        {
            var action = new ListAction();
            int firstCount = 0;
            int secondCount = 0;
            Action first = () => firstCount++;
            Action second = () =>
            {
                secondCount++;
                action.Unsubscribe(first);
            };

            action.Subscribe(first);
            action.Subscribe(second);
            action.Invoke();

            Require(firstCount == 1 && secondCount == 1,
                "已经执行过的回调不应该因为后续移除而再次执行");
            Require(action.Count == 1, "移除已执行回调后应该只剩一个回调");
        });

        RunCase("LinkedAction_StaticDelegate_DuplicateIsIgnored", () =>
        {
            var action = new ListAction();

            action.Subscribe(StaticNoOp);
            action.Subscribe(StaticNoOp);

            Require(action.Count == 1, "相同静态方法委托应该去重");
        });

        RunCase("LinkedAction_CapturingDelegates_FromDifferentClosuresAreDistinct", () =>
        {
            var action = new ListAction();
            int callCount = 0;
            Action first = CreateCapturedCallback(() => callCount++);
            Action second = CreateCapturedCallback(() => callCount++);

            action.Subscribe(first);
            action.Subscribe(second);
            action.Invoke();

            Require(action.Count == 2, "不同闭包实例的委托应该分别订阅");
            Require(callCount == 2, "不同闭包实例的委托应该分别执行");
        });

        RunCase("LinkedAction_CallbackException_LogsAndContinues", () =>
        {
            var action = new ListAction();
            bool laterCallbackCalled = false;

            action.Subscribe(() => throw new InvalidOperationException("expected"));
            action.Subscribe(() => laterCallbackCalled = true);

            ZLog.Log("[TEST][EXPECTED-DIAGNOSTIC] 回调异常会记录错误，但不应中断后续回调");
            action.Invoke();

            Require(laterCallbackCalled, "回调异常不应该中断后续回调");
        });

        RunCase("LinkedAction_InvokeRepeatedly_AfterTraversalStateIsReusable", () =>
        {
            var action = new ListAction<int>();
            int callCount = 0;
            action.Subscribe(_ => callCount++);

            action.Invoke(1);
            action.Invoke(2);

            Require(callCount == 2 && action.Count == 1,
                "重复触发后订阅状态和回调次数应该保持正确");
        });

        RunCase("LinkedAction_CallbackException_RemoveAndInvokeAgain_RemainsUsable", () =>
        {
            var action = new ListAction();
            Action failing = () => throw new InvalidOperationException("expected");
            int healthyCount = 0;
            Action healthy = () => healthyCount++;
            action.Subscribe(failing);
            action.Subscribe(healthy);

            ZLog.Log("[TEST][EXPECTED-DIAGNOSTIC] 异常回调会记录错误，委托容器应保持可用");
            action.Invoke();
            Require(healthyCount == 1, "异常回调不应该阻止其他回调执行");
            Require(action.Unsubscribe(failing), "异常回调应该可以被移除");
            action.Invoke();
            Require(healthyCount == 2, "移除异常回调后剩余回调应该可以继续执行");
        });
    }

    private static void StaticNoOp()
    {
    }

    private static Action CreateCapturedCallback(Action callback)
    {
        return () => callback();
    }

    private void RunSuite(string suiteName, Action suite)
    {
        ZLog.Log($"[TEST][SUITE] {suiteName}");

        try
        {
            suite();
        }
        catch (Exception exception)
        {
            _total++;
            _failed++;
            ZLog.LogError($"[TEST][FAIL] {suiteName}: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private void RunCase(string testName, Action test)
    {
        _total++;

        try
        {
            test();
            _passed++;
            ZLog.Log($"[TEST][PASS] {testName}");
        }
        catch (Exception exception)
        {
            _failed++;
            ZLog.LogError($"[TEST][FAIL] {testName}: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class DelegateTarget
    {
        public int CallCount { get; private set; }

        public void Handle()
        {
            CallCount++;
        }
    }

    private sealed class TypedDelegateTarget
    {
        public int CallCount { get; private set; }

        public void Handle(int value)
        {
            CallCount++;
        }
    }

    private sealed class CollectionPoolItem
    {
        public int Value;
    }

    private void RunEventCenterTests()
    {
        EventCenter.Instance.Clear();

        int received = 0;
        Action<TestEvent> listener = data => received += data.Value;

        EventCenter.Instance.AddListener(listener);
        EventCenter.Instance.AddListener(listener);
        EventCenter.Instance.EventTrigger(new TestEvent { Value = 5 });
        Check(received == 5, "EventCenter 防止重复监听");

        EventCenter.Instance.RemoveListener(listener);
        EventCenter.Instance.EventTrigger(new TestEvent { Value = 5 });
        Check(received == 5, "EventCenter 移除监听");
    }

    private void RunListEventTests()
    {
            var linkedEvent = new ListEvent();
        int count = 0;
        Action callback = () => count++;

        linkedEvent.Invoked += callback;
        linkedEvent.Invoked += callback;
        linkedEvent.Invoke();
        Check(count == 1 && linkedEvent.Count == 1, "LinkedEvent 防止重复订阅");

        linkedEvent.Invoked -= callback;
        linkedEvent.Invoke();
        Check(count == 1 && linkedEvent.Count == 0, "LinkedEvent 取消订阅");

        var typedEvent = new ListEvent<int>();
        int sum = 0;
        Action<int> first = value => sum += value;
        Action<int> second = value => sum += value * 10;

        typedEvent.Invoked += first;
        typedEvent.Invoked += second;
        typedEvent.Invoke(2);
        Check(sum == 22 && typedEvent.Count == 2, "泛型 LinkedEvent 传递参数");

        typedEvent.Invoked -= first;
        typedEvent.Invoke(2);
        Check(sum == 42 && typedEvent.Count == 1, "泛型 LinkedEvent 移除指定回调");

        var mutationEvent = new ListEvent<int>();
        int mutationCount = 0;
        Action<int> selfRemove = null;
        selfRemove = _ =>
        {
            mutationCount++;
            mutationEvent.Invoked -= selfRemove;
        };

        mutationEvent.Invoked += selfRemove;
        mutationEvent.Invoked += _ => mutationCount += 10;
        mutationEvent.Invoke(0);
        mutationEvent.Invoke(0);
        Check(mutationCount == 21, "LinkedEvent 回调执行过程中移除自身");

        typedEvent.Clear();
        Check(typedEvent.Count == 0, "LinkedEvent 清空回调");

        var reusableEvent = new ListEvent();
        int reusableCount = 0;
        reusableEvent.Invoked += () => reusableCount++;
        reusableEvent.Invoke();
        reusableEvent.Invoke();
        Check(reusableCount == 2 && reusableEvent.Count == 1,
            "LinkedEvent 重复触发后遍历状态应该可复用");
    }

    private void Check(bool condition, string testName)
    {
        _total++;

        if (condition)
        {
            _passed++;
            ZLog.Log($"[TEST][PASS] {testName}");
        }
        else
        {
            _failed++;
            ZLog.LogError($"[TEST][FAIL] {testName}");
        }
    }

    private struct TestEvent
    {
        public int Value;
    }
}
