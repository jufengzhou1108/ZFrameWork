using System;
using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>
    /// 为响应式数据提供按类型缓存的值比较。
    /// Unity 数学结构体（Vector2/3/4、Quaternion、Color 等）均实现 IEquatable 且 Equals 为精确比较，
    /// 默认比较器即精确语义，无需特判；未来若有默认比较不满足的类型，在 switch 中加分支即可。
    /// </summary>
    public static class ValueComparer<T>
    {
        private static readonly Func<T, T, bool> Comparer = CreateComparer();

        public static bool Compare(T data1, T data2)
        {
            return Comparer(data1, data2);
        }

        private static Func<T, T, bool> CreateComparer()
        {
            return typeof(T) switch
            {
                _ => EqualityComparer<T>.Default.Equals,
            };
        }
    }
}
