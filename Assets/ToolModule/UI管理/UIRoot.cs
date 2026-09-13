using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZFrameWork
{
    /// <summary>
    /// UI 根节点，全局唯一且不随场景切换销毁。
    /// Awake 时按 UIManagerConfig 的层级配置创建各层 Canvas 与管理器，并兜底创建 EventSystem。
    /// 资源组工厂由游戏入口通过 SetResourceGroupFactory 注入。
    /// </summary>
    public sealed class UIRoot : SingletonAutoMono<UIRoot>
    {
        private static Func<IResourceGroup> _resourceGroupFactory;
        private readonly List<UIManager> _managers = new();

        /// <summary>注入全局资源组工厂，须在 UIRoot 首次访问前调用。</summary>
        public static void SetResourceGroupFactory(Func<IResourceGroup> factory)
        {
            _resourceGroupFactory = factory;
        }

        private void Awake()
        {
            EnsureEventSystem();
            CreateManagers();
        }

        /// <summary>场景切换时由场景管理模块显式调用，自上而下清理各层界面。</summary>
        public void CleanupForSceneSwitch()
        {
            for (int i = _managers.Count - 1; i >= 0; i--)
                _managers[i].ClearAll();
        }

        private void CreateManagers()
        {
            Type[] types = UIManagerConfig.ManagerTypes;
            for (int i = 0; i < types.Length; i++)
            {
                if (Activator.CreateInstance(types[i]) is UIManager manager)
                {
                    manager.Bind(CreateCanvas($"{types[i].Name}Canvas", i * 100), _resourceGroupFactory);
                    _managers.Add(manager);
                }
                else
                {
                    ZLog.LogError($"[UIRoot] UIManagerConfig 中的类型不是 UIManager：{types[i].Name}");
                }
            }
        }

        private Canvas CreateCanvas(string canvasName, int sortingOrder)
        {
            GameObject canvasObject = new GameObject(canvasName);
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
                return;

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(transform, false);
        }
    }
}
