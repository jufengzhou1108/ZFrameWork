using System;
using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>
    /// 界面路径配置。类型到 Addressables 路径的映射，新增界面时在此注册。
    /// 加载时未注册直接报错。
    /// </summary>
    public static class UIViewConfig
    {
        /// <summary>类型到路径的映射。internal 供同程序集（如 Play 测试）按需补充条目。</summary>
        internal static readonly Dictionary<Type, string> Paths = new Dictionary<Type, string>
        {
            // 示例：
            // { typeof(MainMenuView), "UI/MainMenu" },
            // { typeof(ConfirmPopup), "UI/Popup/ConfirmPopup" },
            
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
