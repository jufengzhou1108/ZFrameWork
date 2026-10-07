using Newtonsoft.Json;
using System.IO;
using UnityEngine;

/// <summary>
/// json工具，可扩展以修改json存储位置
/// </summary>
namespace ZFrameWork
{

public static class JsonTool
{
    private const string JSON_PATH = @"D:\code\DUnity\丧死围城\Assets\ToolModule\数据持久化\Json\";

    /// <summary>
    /// 反序列化指定类
    /// </summary>
    /// <typeparam name="T">传入类，例如PlayerData</typeparam>
    /// <param name="name">类名，例如Player1</param>
    /// <param name="dicPath">文件夹路径</param>
    /// <returns></returns>
    public static T LoadJson<T>(string name,string dicPath= JSON_PATH) where T : class,new()
    {
        if (!Directory.Exists(dicPath))
        {
            ZLog.LogError("文件夹不存在: " + dicPath);
            return default;
        }
        string path = dicPath + typeof(T).Name +"_"+ name + ".json";
        if(!File.Exists(path))
        {
            ZLog.LogError("文件不存在: " + path);
            return default;
        }
        return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
    }

    /// <summary>
    /// 序列化指定类数据
    /// </summary>
    /// <typeparam name="T">类名</typeparam>
    /// <param name="data">序列化数据</param>
    /// <param name="name">文件名</param>
    /// <param name="dicPath">文件夹路径</param>
    public static void SaveData<T>(T data,string name, string dicPath = JSON_PATH) where T : class
    {
        if (!Directory.Exists(dicPath))
        {
            ZLog.LogError("文件夹不存在: " + dicPath);
            return;
        }

        string path = dicPath + typeof(T).Name +"_"+ name + ".json";
        string content=JsonConvert.SerializeObject(data);
        File.WriteAllText(path, content);
    }
}
}
