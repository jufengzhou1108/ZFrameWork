using System;
using System.Threading.Tasks;

namespace ZFrameWork
{
    public sealed class UINode
    {
        public Type Key { get; }

        public UIBase UI { get; internal set; }

        public string Path { get; internal set; }

        public Task LoadTask { get; internal set; }

        public bool TargetExists { get; internal set; }

        public bool TargetVisible { get; internal set; }

        internal bool Removed { get; set; }

        internal System.Collections.Generic.LinkedListNode<UINode> Link { get; set; }

        public UINode(Type key)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }
    }
}
