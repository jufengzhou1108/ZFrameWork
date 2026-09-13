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
    /// 模态拦截由 UIBlocker 实现，始终垫在栈顶弹窗之下。
    /// </summary>
    public sealed class PopupManager : UIManager<PopupManager>
    {
        private readonly LinkedList<UINode> _stack = new();
        private readonly Dictionary<Type, UINode> _nodes = new();
        private UIBlocker _blocker;

        protected override void OnInitialize()
        {
            GameObject blockerObject = new GameObject("PopupBlocker", typeof(RectTransform));
            RectTransform rect = (RectTransform)blockerObject.transform;
            rect.SetParent(Canvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _blocker = blockerObject.AddComponent<UIBlocker>();
            blockerObject.SetActive(false);
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
            SyncBlocker();
        }

        /// <summary>关闭所有弹窗。单例 UI 发生界面切换时由 SingletonUIManager 调用。</summary>
        public void Clear()
        {
            if (!EnsureAlive())
                return;

            while (_stack.Last != null)
                RemoveNode(_stack.Last.Value);
            SyncBlocker();
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
                SyncBlocker();
                return;
            }
            if (!EnsureResourceGroup())
            {
                RemoveNode(node);
                SyncBlocker();
                return;
            }

            GameObject prefab = await ResourceGroup.LoadAsync<GameObject>(path, path);

            if (node.Removed || ResourceGroup.IsCooled)
                return;

            if (prefab == null)
            {
                ZLog.LogError($"[PopupManager] 弹窗加载失败：{node.Key.Name}, path={path}");
                RemoveNode(node);
                SyncBlocker();
                return;
            }

            // 加载成功即持有资源引用，登记到节点上，之后所有失败路径统一由 RemoveNode 释放
            node.Path = path;

            if (prefab.GetComponent<TView>() is TView source)
            {
                TView panel = Object.Instantiate(source);
                node.UI = panel;
                panel.SetKeyInternal(path);
                if (panel.EnsureResourceGroup(ResourceGroupFactory))
                {
                    panel.transform.SetParent(Canvas.transform, false);
                    panel.gameObject.SetActive(false);
                    panel.SetData(data);   // 数据先于生命周期注入
                    panel.OpenInternal();
                    panel.ShowInternal();
                    SyncBlocker();
                    return;
                }
                Object.Destroy(panel.gameObject);
            }
            else
            {
                ZLog.LogError($"[PopupManager] 弹窗预制体缺少 {node.Key.Name} 组件：{path}");
            }

            RemoveNode(node);
            SyncBlocker();
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

        /// <summary>同步拦截器：垫在栈顶弹窗之下（兄弟索引 n-1），栈空时隐藏。</summary>
        private void SyncBlocker()
        {
            // Unity 伪 null：物体销毁后引用仍在，直接使用会抛 MissingReferenceException
            if (_blocker == null || Canvas == null)
            {
                ZLog.LogError("[PopupManager] 拦截器或所在 Canvas 已销毁，无法同步拦截器。");
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
                _blocker.gameObject.SetActive(true);
                _blocker.transform.SetSiblingIndex(_stack.Count - 1);
            }
            else
            {
                _blocker.gameObject.SetActive(false);
            }
        }
    }
}
