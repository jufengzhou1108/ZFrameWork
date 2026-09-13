
using UnityEngine;

namespace ZFrameWork
{

public class SingletonMono<T>:MonoBehaviour where T:class
{
    private static T instance;
    private void Awake()
    {
        instance = GetComponent<T>();
    }

    public static T Instance => instance;
}
}
