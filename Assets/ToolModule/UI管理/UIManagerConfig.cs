using System;

namespace ZFrameWork
{
    /// <summary>
    /// UI 层级配置。数组索引即层级，越往后层级越高（渲染在上方）。
    /// </summary>
    public static class UIManagerConfig
    {
        /// <summary>各层管理器类型，按从低到高的顺序声明。</summary>
        public static readonly Type[] ManagerTypes =
        {
            typeof(SingletonUIManager),
            typeof(PopupManager),
        };
    }
}
