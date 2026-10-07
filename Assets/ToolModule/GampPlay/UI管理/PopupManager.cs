using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZFrameWork
{
    /// <summary>
    /// 弹窗管理器。栈管理 PopupUINode（界面、加载任务、期望状态），
    /// 只提供 PushView（显示并压栈）、PopView（关闭栈顶）、Clear（关闭所有）。
    /// 重复与乱序处理与单例 UI 一致：UINode 同步代表去重、复用加载任务、按目标状态仲裁。
    /// 模态拦截由 RaycastMask 实现，始终垫在栈顶弹窗之下。
    /// </summary>
    public sealed class PopupManager : UIManager<PopupManager>
    {
        private readonly LinkedList<UINode> _stack = new();
        private readonly Dictionary<Type, UINode> _nodes = new();
        private RaycastMask _raycastMask;

        protected override void OnInitialize()
        {
            // 显式带上 CanvasRenderer：RaycastMask 是 Graphic，缺了它射线判定会退化（详见 RaycastMask 注释）
            GameObject maskObject = new GameObject("RaycastMask", typeof(RectTransform), typeof(CanvasRenderer));
            RectTransform rect = (RectTransform)maskObject.transform;
            rect.SetParent(Canvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _raycastMask = maskObject.AddComponent<RaycastMask>();
            maskObject.SetActive(false);
        }

        /// <summary>
        /// 压入弹窗：异步加载，数据在 Open 之前注入，完成后显示。
        /// 已在栈中（含加载中）时拒绝重复请求。
        /// </summary>
        public bool Push<TView, TData>(TData data) where TView : PopupBase<TData>
        {
            if (!EnsureAlive())
                return false;

            Type key = typeof(TView);
            if (_nodes.TryGetValue(key, out UINode existing) && !existing.Removed)
            {
                ZLog.LogWarning($"[PopupManager] 弹窗已在栈中，拒绝重复 Push：{key.Name}");
                return false;
            }

            UINode node = new UINode(key)
            {
                TargetExists = true,
                TargetVisible = true,
            };
            node.Link = _stack.AddLast(node);
            _nodes[key] = node;
            node.LoadTask = LoadNodeAsync<TView, TData>(node, data);
            return true;
        }

        /// <summary>关闭栈顶弹窗。</summary>
        public void PopView()
        {
            if (!EnsureAlive())
                return;

            UINode top = _stack.Last?.Value;
            if (top == null)
                return;

            RemoveNode(top);
            SyncRaycastMask();
        }

        /// <summary>关闭所有弹窗。单例 UI 发生界面切换时由 SingletonUIManager 调用。</summary>
        public void Clear()
        {
            if (!EnsureAlive())
                return;

            while (_stack.Last != null)
                RemoveNode(_stack.Last.Value);
            SyncRaycastMask();
        }

        internal override void ClearAll()
        {
            Clear();
            ReleaseResourceGroup();
        }

        public UINode Find(Type viewType)
        {
            return _nodes.TryGetValue(viewType, out UINode node) ? node : null;
        }

        private async Task LoadNodeAsync<TView, TData>(UINode node, TData data)
            where TView : PopupBase<TData>
        {
            if (!UIViewConfig.TryGetPath(node.Key, out string path))
            {
                RemoveNode(node);
                SyncRaycastMask();
                return;
            }
            if (!EnsureResourceGroup())
            {
                RemoveNode(node);
                SyncRaycastMask();
                return;
            }

            GameObject prefab = await ResourceGroup.LoadAsync<GameObject>(path, path);

            if (node.Removed)
                return;

            if (prefab == null)
            {
                ZLog.LogError($"[PopupManager] 弹窗加载失败：{node.Key.Name}, path={path}");
                RemoveNode(node);
                SyncRaycastMask();
                return;
            }

            // 加载成功即持有资源引用，登记到节点上，之后所有失败路径统一由 RemoveNode 释放
            node.Path = path;

            if (prefab.GetComponent<TView>() is TView source)
            {
                TView panel = Object.Instantiate(source);
                node.UI = panel;
                panel.SetKeyInternal(path);
                if (panel.EnsureResourceGroup())
                {
                    panel.transform.SetParent(Canvas.transform, false);
                    panel.gameObject.SetActive(false);
                    panel.SetData(data);   // 数据先于生命周期注入
                    panel.OpenInternal();
                    panel.ShowInternal();
                    SyncRaycastMask();
                    return;
                }
                Object.Destroy(panel.gameObject);
            }
            else
            {
                ZLog.LogError($"[PopupManager] 弹窗预制体缺少 {node.Key.Name} 组件：{path}");
            }

            RemoveNode(node);
            SyncRaycastMask();
        }

        private void RemoveNode(UINode node)
        {
            node.Removed = true;
            node.TargetExists = false;
            node.TargetVisible = false;
            node.LoadTask = null;

            if (node.Link != null)
            {
                _stack.Remove(node.Link);
                node.Link = null;
            }
            if (_nodes.TryGetValue(node.Key, out UINode current) && current == node)
                _nodes.Remove(node.Key);

            if (node.UI != null)
            {
                node.UI.CloseInternal();
                Object.Destroy(node.UI.gameObject);
                node.UI = null;
            }
            if (!string.IsNullOrEmpty(node.Path))
                ResourceGroup?.Release<GameObject>(node.Path);
            node.Path = null;
        }

        /// <summary>同步射线遮罩：垫在栈顶弹窗之下（兄弟索引 n-1），栈空时隐藏。</summary>
        private void SyncRaycastMask()
        {
            // Unity 伪 null：物体销毁后引用仍在，直接使用会抛 MissingReferenceException
            if (_raycastMask == null || Canvas == null)
            {
                ZLog.LogError("[PopupManager] 射线遮罩或所在 Canvas 已销毁，无法同步射线遮罩。");
                return;
            }

            int index = 0;
            for (LinkedListNode<UINode> link = _stack.First; link != null; link = link.Next)
            {
                if (link.Value.UI != null)
                    link.Value.UI.transform.SetSiblingIndex(index++);
            }

            if (_stack.Count > 0)
            {
                _raycastMask.gameObject.SetActive(true);
                _raycastMask.transform.SetSiblingIndex(_stack.Count - 1);
            }
            else
            {
                _raycastMask.gameObject.SetActive(false);
            }
        }
    }
}
