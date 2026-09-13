using System;
using UnityEngine;


//����mono
//1.����Ϊ��mono�ṩЭ�����
//2.��������ִ�и��º���������������
//3.����Ϊ��mono�ṩ���ں���
namespace ZFrameWork
{

public class PublicMono : SingletonAutoMono<PublicMono>
{
    private readonly ListAction updateActions = new();

    public void AddUpdateAction<T>(Action action)
    {
        updateActions.Subscribe(action);
    }

    public void RemoveUpdateAction<T>(Action action)
    {
        updateActions.Unsubscribe(action);
    }

    private void Update()
    {
        updateActions.Invoke();
    }
}
}
