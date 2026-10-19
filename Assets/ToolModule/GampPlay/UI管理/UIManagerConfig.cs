using System;
using UnityEngine;

namespace ZFrameWork
{
    public static class UIManagerConfig
    {
        public static readonly Type[] ManagerTypes =
        {
            typeof(SingletonUIManager),
            typeof(PopupManager),
        };

        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    }
}
