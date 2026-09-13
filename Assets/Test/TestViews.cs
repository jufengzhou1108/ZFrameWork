using System.Collections.Generic;
using UnityEngine;

namespace ZFrameWork.PlayTests
{
    /// <summary>记录生命周期 On 调用序列的测试界面基类。</summary>
    public abstract class CountingView : UIBase
    {
        /// <summary>本实例收到的 On 调用序列，如 "Open","Show"。</summary>
        public readonly List<string> Calls = new();

        /// <summary>OnHide 执行时物体是否仍处于激活状态（验证时序约定）。</summary>
        public bool? ActiveSelfDuringOnHide;

        protected override void OnOpen() => Calls.Add("Open");
        protected override void OnShow() => Calls.Add("Show");
        protected override void OnHide()
        {
            ActiveSelfDuringOnHide = gameObject.activeSelf;
            Calls.Add("Hide");
        }
        protected override void OnClose() => Calls.Add("Close");
    }

    public sealed class TestViewA : CountingView { }
    public sealed class TestViewB : CountingView { }
    public sealed class TestViewC : CountingView { }

    public sealed class TestPopupA : PopupBase<TestPopupA.Data>
    {
        public new readonly struct Data
        {
            public readonly int Value;
            public readonly string Label;
            public Data(int value, string label)
            {
                Value = value;
                Label = label;
            }
        }

        /// <summary>OnShow 时读到的 Data.Value，验证注入时机。</summary>
        public int? ValueSeenOnShow;

        protected override void OnShow()
        {
            ValueSeenOnShow = base.Data.Value; // 嵌套类型 Data 遮蔽同名属性，需 base 访问
        }
    }

    public sealed class TestPopupB : PopupBase<TestPopupB.Data>
    {
        public new readonly struct Data
        {
            public readonly bool Flag;
            public Data(bool flag) { Flag = flag; }
        }
    }
}
