using System;
using System.Threading.Tasks;

namespace ZFrameWork
{
    /// <summary>
    /// UI 栈节点，界面的异步同步代表。
    /// 记录 UI 实例、目标状态（显示/隐藏、存在/不存在）与在途加载任务，
    /// 异步操作完成后依据节点记录的目标状态决定处理方式，解决乱序与重复请求问题。
    /// </summary>
    public sealed class UINode
    {
        /// <summary>界面对应的类型，字典与日志的 key。</summary>
        public Type Key { get; }

        /// <summary>已实例化的 UI，加载完成前为 null。</summary>
        public UIBase UI { get; internal set; }

        /// <summary>预制体资源路径，同时作为资源组内的加载 key，关闭时用于释放。</summary>
        public string Path { get; internal set; }

        /// <summary>在途的加载任务，重复请求复用它，无加载时为 null。</summary>
        public Task LoadTask { get; internal set; }

        /// <summary>目标状态：节点是否应存在。置否后加载完成回调不再实例化。</summary>
        public bool TargetExists { get; internal set; }

        /// <summary>目标状态：界面是否应显示。加载完成后按此对齐可见性。</summary>
        public bool TargetVisible { get; internal set; }

        /// <summary>节点是否已被管理器移除。在途加载完成后检查此标记，作废迟到结果。</summary>
        internal bool Removed { get; set; }

        /// <summary>栈链表节点，栈顶为链表尾部。</summary>
        internal System.Collections.Generic.LinkedListNode<UINode> Link { get; set; }

        public UINode(Type key)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }
    }
}
