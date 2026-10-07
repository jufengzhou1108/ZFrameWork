using Cysharp.Threading.Tasks;
using UnityEngine;
using ZFrameWork;

/// <summary>
/// Play 模式测试入口：把本组件挂到场景物体上，进入 Play 后跑对象池套件两遍（查单例泄漏）。
/// </summary>
public class Test : MonoBehaviour
{
    private static bool hasRun;

    private void Start()
    {
        if (hasRun)
        {
            return;
        }

        hasRun = true;
        Run().Forget();
    }

    private static async UniTaskVoid Run()
    {
        await PoolTests.RunAll();
        await PoolTests.RunAll();
    }
}
