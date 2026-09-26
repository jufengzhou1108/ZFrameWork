using System;
using UnityEngine;

namespace ZFrameWork.PlayTests
{
    /// <summary>
    /// Play 模式测试入口。把本组件挂到当前场景任意物体上，进入 Play 模式即自动运行全部测试，
    /// Console 中以 [TEST][PASS]/[TEST][FAIL]/[TEST][SUITE] 前缀输出结果与最终汇总。
    /// </summary>
    public class Test : MonoBehaviour
    {
        private static bool _executed;

        private void Start()
        {
            if (_executed)
            {
                Debug.Log("[TEST][SUITE] Test 已在本次会话运行过，跳过重复执行。");
                return;
            }
            _executed = true;

            try
            {
                PoolTests.RunAll();
                StateMachineTests.RunAll();
            }
            catch (Exception e)
            {
                Debug.LogError($"[TEST][FAIL] 套件级异常: {e}");
            }
        }
    }
}
