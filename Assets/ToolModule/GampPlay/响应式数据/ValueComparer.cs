using System;
using System.Collections.Generic;

namespace ZFrameWork
{
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
