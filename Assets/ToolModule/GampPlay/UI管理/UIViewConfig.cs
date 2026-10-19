using System;
using System.Collections.Generic;

namespace ZFrameWork
{
    public static class UIViewConfig
    {
        internal static readonly Dictionary<Type, string> Paths = new Dictionary<Type, string>
        {
            
        };

        public static bool TryGetPath(Type viewType, out string path)
        {
            if (Paths.TryGetValue(viewType, out path))
                return true;

            ZLog.LogError($"[UIViewConfig] 界面类型未注册路径：{viewType.Name}");
            return false;
        }
    }
}
