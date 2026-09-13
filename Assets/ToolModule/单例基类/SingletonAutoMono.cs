using UnityEngine;

/// <summary>
/// ��mono����,���Զ�����,����ʵ�ֳ��ڹ�����
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
