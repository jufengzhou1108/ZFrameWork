using System;
using UnityEngine;

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

        /// <summary>
        /// UI 设计稿分辨率。各层 Canvas 用"随屏幕大小缩放 + Expand"：
        /// 设计稿这块矩形在任何屏幕上都完整可见，多出来的空间由锚点分配。
        /// 画布局部坐标系的尺寸 = 屏幕像素 ÷ 缩放系数，所以这个值就是摆放界面时用的设计坐标范围。
        /// </summary>
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    }
}
