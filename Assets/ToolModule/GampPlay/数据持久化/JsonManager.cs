using Newtonsoft.Json;
using System.IO;
using UnityEngine;

namespace ZFrameWork
{

public static class JsonTool
{
    private const string JSON_PATH = @"D:\code\DUnity\丧死围城\Assets\ToolModule\数据持久化\Json\";

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
