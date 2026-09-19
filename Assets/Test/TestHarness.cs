using System;
using System.Threading.Tasks;
using UnityEngine;

namespace ZFrameWork.PlayTests
{
    /// <summary>Play 模式测试基础设施：用例隔离、断言、异步等待与汇总。</summary>
    public static class TestHarness
    {
        public static int Total;
        public static int Passed;
        public static int Failed;
        public static int Skipped;

        /// <summary>用例隔离：单条用例抛异常只记一次失败，后续用例照常执行。</summary>
        public static async Task RunCaseAsync(string name, Func<Task> body)
        {
            Total++;
            try
            {
                await body();
                Passed++;
                Debug.Log($"[TEST][PASS] {name}");
            }
            catch (OperationCanceledException)
            {
                // 中止信号（Play 模式退出）：向上传播终止整套测试，不计为失败
                throw;
            }
            catch (Exception e)
            {
                Failed++;
                Debug.Log($"[TEST][FAIL] {name} | {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>重置计数器。每个套件 RunAll 开头必须调用，否则后跑的套件会把前面套件的用例算进自己的汇总。</summary>
        public static void Reset()
        {
            Total = 0;
            Passed = 0;
            Failed = 0;
            Skipped = 0;
        }

        public static void Skip(string name, string reason)
        {
            Total++;
            Skipped++;
            Debug.Log($"[TEST][SKIP] {name} | 原因: {reason}");
        }

        /// <summary>断言，失败抛异常记为该用例失败，不中断整套测试。</summary>
        public static void Require(bool condition, string message)
        {
            if (!condition)
                throw new Exception($"断言失败: {message}");
        }

        /// <summary>轮询等待条件成立，超时视为该用例失败。Task.Delay 的延续在主线程泵帧，因此能跨帧。</summary>
        public static async Task WaitFor(Func<bool> condition, int timeoutMs = 5000, string what = null)
        {
            int waited = 0;
            while (!condition())
            {
                await Task.Delay(10);
                waited += 10;
                if (waited >= timeoutMs)
                    throw new Exception($"等待超时({timeoutMs}ms): {what ?? condition.Method.Name}");
            }
        }

        public static void Summary(string suiteName)
        {
            Debug.Log($"[TEST][SUITE] {suiteName} | 总数: {Total} 通过: {Passed} 失败: {Failed} 跳过: {Skipped}");
            Debug.Log(TestHarness.Failed == 0
                ? $"[TEST][SUITE] {suiteName} 最终结论: 全部通过"
                : $"[TEST][SUITE] {suiteName} 最终结论: 存在 {Failed} 个失败用例");
        }
    }
}
