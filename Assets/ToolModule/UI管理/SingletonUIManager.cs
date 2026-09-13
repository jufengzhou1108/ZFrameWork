using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZFrameWork
{
    /// <summary>
    /// 单例 UI 栈式管理器。链表维护界面栈（尾部为栈顶），
    /// UINode 作为同步代表处理异步乱序与重复请求。
    /// 语义：
    /// Open  —— 入栈并异步加载，保持隐藏；
    /// Show  —— 栈中已有则出栈到该界面再显示，没有则先 Open 后 Show；
    /// Hide  —— 仅隐藏界面，不动栈；
    /// Close —— 栈顶则移除并显示上一个，非栈顶则从中间移除且不打乱顺序。
    /// </summary>
    public sealed class SingletonUIManager : UIManager<SingletonUIManager>
    {
        private readonly LinkedList<UINode> _stack = new();
        private readonly Dictionary<Type, UINode> _nodes = new();

        /// <summary>打开界面：入栈并异步加载，加载完成前不显示。已在栈中时仅对齐目标状态为隐藏。</summary>
        public bool Open(Type viewType)
        {
            if (!EnsureAlive())
                return false;

            SetNode(viewType, targetVisible: false);
            return true;
        }

        public bool Open<TView>() where TView : UIBase => Open(typeof(TView));

        /// <summary>
        /// 设置界面的目标状态，节点有无由内部统一处理：
        /// 已存在则对齐目标与可见性（重复请求去重，不重复加载）；
        /// 不存在则创建节点并入栈——目标先于加载触发设置，
        /// 保证加载同步完成时回调读到的目标即最终目标。
        /// </summary>
        /// <summary>对齐已存在节点的目标与可见性。节点不存在返回 false，无副作用。</summary>
        private bool TryAlignNode(Type viewType, bool targetVisible)
        {
            if (!_nodes.TryGetValue(viewType, out UINode existing))
                return false;

            existing.TargetVisible = targetVisible;
            if (existing.UI != null)
            {
                if (targetVisible)
                    existing.UI.ShowInternal();
                else if (existing.UI.IsShown)
                    existing.UI.HideInternal();
            }
            return true;
        }

        private void SetNode(Type viewType, bool targetVisible)
        {
            if (TryAlignNode(viewType, targetVisible))
                return;

            ClearPopupsForSwitch();
            UINode node = new UINode(viewType)
            {
                TargetExists = true,
                TargetVisible = targetVisible,
            };
            PushNode(node);
            node.LoadTask = LoadNodeAsync(node);
        }

        /// <summary>显示界面：栈中已有则出栈到该界面，没有则创建节点（目标即显示）。</summary>
        public bool Show(Type viewType)
        {
            if (!EnsureAlive())
                return false;

            if (!_nodes.TryGetValue(viewType, out UINode node))
            {
                SetNode(viewType, targetVisible: true);
                return true;
            }

            bool switching = _stack.Last == null || _stack.Last.Value != node;
            if (switching)
                ClearPopupsForSwitch();

            // 出栈到目标界面：关闭其上所有界面，不打乱剩余顺序
            while (_stack.Last != null && _stack.Last.Value != node)
                RemoveNode(_stack.Last.Value);

            SetNode(viewType, targetVisible: true);
            return true;
        }

        public bool Show<TView>() where TView : UIBase => Show(typeof(TView));

        /// <summary>隐藏界面：仅对齐目标为隐藏，栈结构不变。界面未打开时忽略，不创建节点。</summary>
        public void Hide(Type viewType)
        {
            if (!EnsureAlive())
                return;

            TryAlignNode(viewType, targetVisible: false);
        }

        public void Hide<TView>() where TView : UIBase => Hide(typeof(TView));

        /// <summary>关闭界面：栈顶则移除并显示上一个界面，非栈顶则从中间移除。</summary>
        public void Close(Type viewType)
        {
            if (!EnsureAlive())
                return;

            if (!_nodes.TryGetValue(viewType, out UINode node))
                return;

            bool wasTop = _stack.Last != null && _stack.Last.Value == node;
            if (wasTop)
                ClearPopupsForSwitch();

            RemoveNode(node);

            if (!wasTop)
                return;

            UINode top = _stack.Last?.Value;
            if (top != null)
            {
                top.TargetVisible = true;
                if (top.UI != null)
                    top.UI.ShowInternal();
            }
        }

        public void Close<TView>() where TView : UIBase => Close(typeof(TView));

        /// <summary>关闭所有界面，清空栈。</summary>
        public void Clear()
        {
            if (!EnsureAlive())
                return;

            while (_stack.Last != null)
                RemoveNode(_stack.Last.Value);
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

        private async Task LoadNodeAsync(UINode node)
        {
            if (!UIViewConfig.TryGetPath(node.Key, out string path))
            {
                RemoveNode(node);
                return;
            }
            if (!EnsureResourceGroup())
            {
                RemoveNode(node);
                return;
            }

            GameObject prefab = await ResourceGroup.LoadAsync<GameObject>(path, path);

            // 等待期间被 Close：节点已移除，作废迟到结果
            if (node.Removed)
                return;

            if (prefab == null)
            {
                ZLog.LogError($"[SingletonUIManager] 界面加载失败：{node.Key.Name}, path={path}");
                RemoveNode(node);
                return;
            }

            // 加载成功即持有资源引用，登记到节点上，之后所有失败路径统一由 RemoveNode 释放
            node.Path = path;

            UIBase source = prefab.GetComponent<UIBase>();
            if (source == null)
            {
                ZLog.LogError($"[SingletonUIManager] 预制体缺少 UIBase 组件：{node.Key.Name}, path={path}");
                RemoveNode(node);
                return;
            }

            UIBase panel = Object.Instantiate(source);
            node.UI = panel;
            panel.SetKeyInternal(path);
            if (!panel.EnsureResourceGroup(ResourceGroupFactory))
            {
                Object.Destroy(panel.gameObject);
                RemoveNode(node);
                return;
            }

            panel.transform.SetParent(Canvas.transform, false);
            panel.gameObject.SetActive(false);
            panel.OpenInternal();

            // 异步竞态仲裁：按节点的目标状态而非到达顺序决定可见性
            if (node.TargetVisible)
                panel.ShowInternal();
        }

        private void PushNode(UINode node)
        {
            node.Link = _stack.AddLast(node);
            _nodes.Add(node.Key, node);
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

        /// <summary>界面切换时清理弹窗层，在触发切换的操作一开始调用。</summary>
        private static void ClearPopupsForSwitch()
        {
            PopupManager.Instance?.Clear();
        }
    }
}
