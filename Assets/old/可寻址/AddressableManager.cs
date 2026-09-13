using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;


/// <summary>
/// Addressable��صĹ�����
/// </summary>
namespace ZFrameWork
{

public class AddressableManager :Singleton<AddressableManager>
{ 
    //��ԴȨ���ֵ�
    private Dictionary<string,AsyncOperationHandle> handleDic=new Dictionary<string, AsyncOperationHandle>();
    //��Դ�����ֵ�
    private Dictionary<string, int> numDic = new Dictionary<string, int>();

    /// <summary>
    /// �첽���ؿ�Ѱַ��Դ
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name">��Դ��</param>
    /// <param name="action">��Դ���صĻص�����</param>
    public void LoadResAsync<T>(string name, UnityAction<T> action) where T : UnityEngine.Object
    { 
        string key=name+"_"+typeof(T).Name;

        AsyncOperationHandle handle;
        if (!handleDic.ContainsKey(key))
        {
            handle= Addressables.LoadAssetAsync<T>(name);
            handleDic.Add(key, handle);
            numDic.Add(key, 0);
        }
        handle=handleDic[key];
        numDic[key]++;

        //���δ������ֻ���ӻص�
        if (!handle.IsDone)
        {
            handle.Completed += (temHandle) =>
            {
                if (temHandle.Status == AsyncOperationStatus.Failed)
                {
                    ZLog.LogError("资源加载失败: " + key);
                    return;
                }
                action?.Invoke(temHandle.Result as T);
            };
            return;
        }

        if (handle.Status == AsyncOperationStatus.Failed)
        {
            ZLog.LogError("资源加载失败: " + key);
            return;
        }
        action?.Invoke(handle.Result as T);
    }

    /// <summary>
    /// ͬ�����ؿ�Ѱַ��Դ
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name">��Դ��</param>
    /// <returns></returns>
    public T LoadRes<T>(string name) where T : UnityEngine.Object
    {
        string key = name + "_" + typeof(T).Name;

        AsyncOperationHandle handle;
        if (!handleDic.ContainsKey(key))
        {
            handle = Addressables.LoadAssetAsync<T>(name);
            handleDic.Add(key, handle);
            numDic.Add(key, 0);
        }
        handle = handleDic[key];
        numDic[key]++;

        //���δ��������ȴ��������
        if (!handle.IsDone)
        {
            handle.WaitForCompletion();
        }

        if (handle.Status == AsyncOperationStatus.Failed)
        {
            ZLog.LogError("资源加载失败: " + key);
            return default;
        }
        return handleDic[key].Result as T;
    }

    //�ͷ���Դ
    public void Release<T>(string name) where T : UnityEngine.Object
    {
        string key= name + "_" + typeof(T).Name;

        if (!numDic.ContainsKey(key))
        {
            return;
        }

        numDic[key]--;
        if (numDic[key] <= 0)
        {
            handleDic[key].Release();
            handleDic.Remove(key);
            numDic.Remove(key);
        }
    }

    //�����Դ
    public void Clear()
    {
        foreach(AsyncOperationHandle handle in handleDic.Values)
        {
            handle.Release();
        }

        handleDic.Clear();
        numDic.Clear();
    }
}
}
