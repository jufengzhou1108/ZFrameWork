using System;
using UnityEngine;

namespace ZFrameWork.PlayTests
{
    /// <summary>
    /// Play 模式测试基础设施：用例隔离、断言与汇总。
    /// 本批套件全部是同步逻辑用例，因此只提供同步 RunCase。
    /// </summary>
    public static class TestHarness
    {
        public static int Total;
        public static int Passed;
        public static int Failed;
        public static int Skipped;

        /// <summary>用例隔离：单条用例抛异常只记一次失败，后续用例照常执行。</summary>
        public static void RunCase(string name, Action body)
        {
            Total++;
            try
            {
                body();
                Passed++;
                Debug.Log($"[TEST][PASS] {name}");
            }
            catch (Exception e)
            {
                Failed++;
                Debug.Log($"[TEST][FAIL] {name} | {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>重置计数器。每个套件 RunAll 开头必须调用，否则汇总串台。</summary>
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

        public static void Summary(string suiteName)
        {
            Debug.Log($"[TEST][SUITE] {suiteName} | 总数: {Total} 通过: {Passed} 失败: {Failed} 跳过: {Skipped}");
            Debug.Log(Failed == 0
                ? $"[TEST][SUITE] {suiteName} 最终结论: 全部通过"
                : $"[TEST][SUITE] {suiteName} 最终结论: 存在 {Failed} 个失败用例");
        }
    }
}
