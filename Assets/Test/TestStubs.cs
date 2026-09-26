using System;
using System.Collections.Generic;

namespace ZFrameWork.PlayTests
{
    // ---- 对象池桩 ----

    /// <summary>被池化的普通类：只带一个值，用来验证"重置"是否真的作用到同一个实例上。</summary>
    public sealed class PooledThing
    {
        public int Value;
    }

    /// <summary>
    /// 可控时钟的池。Pool.GetCurrentTime 是 protected virtual（文档写明子类可重写为 Time.time），
    /// 测试用它把时间基准接管过来，过期清理才能确定性验证——直接给 CleanExpired 传 now 是不行的，
    /// 因为入池时间来自池自己的时钟。
    /// </summary>
    public sealed class FakeClockPool : Pool<PooledThing>
    {
        public float Now;

        public FakeClockPool(Func<PooledThing> createFunc, Action<PooledThing> destroyAction, Action<PooledThing> resetAction)
            : base(createFunc, destroyAction, resetAction)
        {
        }

        protected override float GetCurrentTime()
        {
            return Now;
        }
    }

    /// <summary>池回调工厂是单例且不提供注销，每个用例各用一个独立类型才能互不干扰。</summary>
    public sealed class FactoryThingA
    {
        public int Value;
    }

    public sealed class FactoryThingB
    {
        public int Value;
    }

    /// <summary>供"重复注册"用例使用的独立类型。</summary>
    public sealed class FactoryThingC
    {
        public int Value;
    }

    /// <summary>工厂里始终未注册的类型，用来验证空委托兜底。</summary>
    public sealed class FactoryThingUnregistered
    {
        public int Value;
    }

    // ---- 状态机桩 ----

    /// <summary>状态机测试共用的调用记录（ChangeState/Init 内部 new 状态，只能靠静态传递）。</summary>
    public static class StateMachineTestLog
    {
        public static readonly List<string> Entries = new();

        public static void Clear()
        {
            Entries.Clear();
        }

        public static string Joined => string.Join(",", Entries);

        public static int CountOf(string entry)
        {
            int count = 0;
            foreach (string item in Entries)
            {
                if (item == entry)
                    count++;
            }
            return count;
        }

        public static int IndexOf(string entry)
        {
            return Entries.IndexOf(entry);
        }
    }

    /// <summary>记录构造与生命周期调用的状态基类。</summary>
    public abstract class RecordingState : StateBase
    {
        protected RecordingState()
        {
            StateMachineTestLog.Entries.Add($"New:{GetType().Name}");
        }

        public override void Enter()
        {
            StateMachineTestLog.Entries.Add($"Enter:{GetType().Name}");
        }

        public override void Exit()
        {
            StateMachineTestLog.Entries.Add($"Exit:{GetType().Name}");
        }

        public override void Update()
        {
            StateMachineTestLog.Entries.Add($"Update:{GetType().Name}");
        }
    }

    // A–K 共 11 个状态：用来验证"状态不设上限、不会被淘汰重建"（旧实现上限为 10）
    public sealed class StateA : RecordingState { }
    public sealed class StateB : RecordingState { }
    public sealed class StateC : RecordingState { }
    public sealed class StateD : RecordingState { }
    public sealed class StateE : RecordingState { }
    public sealed class StateF : RecordingState { }
    public sealed class StateG : RecordingState { }
    public sealed class StateH : RecordingState { }
    public sealed class StateI : RecordingState { }
    public sealed class StateJ : RecordingState { }
    public sealed class StateK : RecordingState { }

    /// <summary>在 Enter 里通过状态机黑板留数据的探针状态，用来验证状态能拿到黑板。</summary>
    public sealed class BlackBoardProbeState : RecordingState
    {
        public override void Enter()
        {
            base.Enter();
            _stateMachine.BlackBoard.SetData(42, "answer");
        }
    }
}
