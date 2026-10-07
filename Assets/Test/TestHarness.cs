using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ZFrameWork
{
    /// <summary>
    /// Play 模式测试脚手架：同步/异步用例隔离、Realtime 等待、统一摘要。
    /// 断言失败抛异常，由 RunCase / RunAsyncCase 记为 FAIL 并继续后续用例。
    /// </summary>
    public static class TestHarness
    {
        private static int total;
        private static int passed;
        private static int failed;
        private static int skipped;

        public static void Reset()
        {
            total = 0;
            passed = 0;
            failed = 0;
            skipped = 0;
        }

        public static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
        }

        public static void Skip(string name, string reason)
        {
            total++;
            skipped++;
            Debug.Log($"[TEST][SKIP] {name} :: {reason}");
        }

        public static async UniTask WaitRealtime(int milliseconds)
        {
            if (milliseconds <= 0)
            {
                return;
            }

            await UniTask.Delay(milliseconds, DelayType.Realtime);
        }

        public static void RunCase(string name, Action body)
        {
            total++;
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                body();
                passed++;
                Debug.Log($"[TEST][PASS] {name} ({sw.ElapsedMilliseconds}ms)");
            }
            catch (Exception e)
            {
                failed++;
                Debug.LogError($"[TEST][FAIL] {name} ({sw.ElapsedMilliseconds}ms) {e.GetType().Name}: {e.Message}\n{e}");
            }
        }

        public static async UniTask RunAsyncCase(string name, Func<UniTask> body, int timeoutMs = 5000)
        {
            total++;
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await body().Timeout(TimeSpan.FromMilliseconds(timeoutMs), DelayType.Realtime);
                passed++;
                Debug.Log($"[TEST][PASS] {name} ({sw.ElapsedMilliseconds}ms)");
            }
            catch (Exception e)
            {
                failed++;
                Debug.LogError($"[TEST][FAIL] {name} ({sw.ElapsedMilliseconds}ms) {e.GetType().Name}: {e.Message}\n{e}");
            }
        }

        public static void Summary(string suite)
        {
            Debug.Log($"[TEST][SUITE] {suite}");
            Debug.Log($"[TEST][SUMMARY] total={total}, passed={passed}, failed={failed}, skipped={skipped}");
        }
    }
}
