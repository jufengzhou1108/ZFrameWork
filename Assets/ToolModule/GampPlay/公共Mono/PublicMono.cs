using System;
using UnityEngine;


// 公共 Mono
// 1.为没有 MonoBehaviour 的类提供协程宿主
// 2.每帧驱动注册进来的更新回调
// 3.常驻不随场景销毁（继承自 SingletonAutoMono）
namespace ZFrameWork
{

public class PublicMono : SingletonAutoMono<PublicMono>
{
    private readonly ListAction updateActions = new();

    public void AddUpdateAction(Action action)
    {
        updateActions.Subscribe(action);
    }

    public void RemoveUpdateAction(Action action)
    {
        updateActions.Unsubscribe(action);
    }

    private void Update()
    {
        updateActions.Invoke();
    }
}
}
