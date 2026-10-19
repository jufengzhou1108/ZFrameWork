using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZFrameWork
{

public class StateBlackBoard
{
    private readonly Dictionary<Type,IDictionary> _dataContainerDic=new Dictionary<Type, IDictionary>();

    public void SetData<T>(T data,string name)
    {
        Type type=typeof(T);
        if (!_dataContainerDic.ContainsKey(type))
        {
            _dataContainerDic.Add(type,new Dictionary<string,T>());
        }

        Dictionary<string,T> containerDic=_dataContainerDic[type] as Dictionary<string,T>;
        if (!containerDic.ContainsKey(name))
        {   
            containerDic.Add(name,data);
            return;
        }

        containerDic[name]=data;
    }

    public void Remove<T>(string name)
    {
        Type type=typeof(T);
        if (!_dataContainerDic.ContainsKey(type))
        {
            return;
        }

        Dictionary<string,T> containerDic=_dataContainerDic[type] as Dictionary<string,T>;
        int hashCode=name.GetHashCode();
        if (!containerDic.ContainsKey(name))
        {
            return;
        }

        containerDic.Remove(name);
        if (containerDic.Count <= 0)
        {
            _dataContainerDic.Remove(type);
        }
    }

    public void Clear()
    {
        _dataContainerDic.Clear();
    }

    public T GetData<T>(string name)
    {
        Type type=typeof(T);
        if (!_dataContainerDic.ContainsKey(type))
        {
            ZLog.LogError($"获取数据失败：数据不存在，type={type},name={name}");
            return default;
        }

        Dictionary<string,T> containerDic=_dataContainerDic[type] as Dictionary<string,T>;
        if (!containerDic.ContainsKey(name))
        {
            ZLog.LogError($"获取数据失败：数据不存在，type={type},name={name}");
            return default;
        }

        return containerDic[name];
    }

    public bool TryGetData<T>(string name,out T data)
    {
        Type type=typeof(T);
        if (!_dataContainerDic.ContainsKey(type))
        {
            data = default(T);
            return false;
        }

        Dictionary<string,T> containerDic=_dataContainerDic[type] as Dictionary<string,T>;
        if (!containerDic.ContainsKey(name))
        {
            data = default(T);
            return false;
        }

        data=containerDic[name];
        return true;
    }
}
}
