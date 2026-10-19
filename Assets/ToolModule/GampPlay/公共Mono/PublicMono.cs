using System;
using UnityEngine;


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
