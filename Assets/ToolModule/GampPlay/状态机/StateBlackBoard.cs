using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZFrameWork
{

public class StateBlackBoard
{
    //键为黑板数据的类型，值为对应的黑板数据字典（键string，值为对应的数据）
    private readonly Dictionary<Type,IDictionary> _dataContainerDic=new Dictionary<Type, IDictionary>();

    /// <summary>
    /// 设置数据
    /// </summary>
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

    /// <summary>
    /// 移除对应数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="name"></param>
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

        //如果容器字典的数据量为0，删除该字典
        containerDic.Remove(name);
        if (containerDic.Count <= 0)
        {
            _dataContainerDic.Remove(type);
        }
    }

    /// <summary>
    /// 清空黑板数据
    /// </summary>
    public void Clear()
    {
        _dataContainerDic.Clear();
    }

    /// <summary>
    /// 获取对应数据 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
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

    /// <summary>
    /// 获取对应数据 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
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
