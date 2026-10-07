using UnityEngine;

/// <summary>
/// Mono 单例基类：首次访问时自动创建物体，并置为 DontDestroyOnLoad 常驻。
/// </summary>
namespace ZFrameWork
{

public class SingletonAutoMono<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance 
    {
        get
        {
            if(instance == null)
            {
                GameObject obj = new GameObject(typeof(T).Name);
                instance = obj.AddComponent<T>();
                GameObject.DontDestroyOnLoad(obj);
            }

            return instance;
        }
    }
}
}
