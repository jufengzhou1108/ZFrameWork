/// <summary>
/// 非 Mono 单例基类：Instance 懒创建，外部不直接 new()。
/// </summary>
namespace ZFrameWork
{

public class Singleton<T> where T : class ,new()
{
    private static T instance;
    public static T Instance
    {
        get 
        {
            if (instance == null)
            {
                instance = new T();
            }

            return instance;
        }
    }
}
}
