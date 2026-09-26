using System;
using UnityEngine;

namespace ZFrameWork.PlayTests
{
    /// <summary>
    /// 状态机模块（StateMachine / StateBase）的 Play 模式测试套件。
    /// 本轮改动重点：Init 由外部传入初始状态（未 Init 就用要报错，不再崩）、状态容器取消上限（不会被淘汰重建）。
    /// 标注"预期诊断"的用例会主动触发一次 ZLog 报错，报错是契约的一部分，断言一律落在状态上。
    /// 调用序列靠 StateMachineTestLog 记录：状态实例由状态机内部 new，测试拿不到实例引用。
    /// </summary>
    public static class StateMachineTests
    {
        public static void RunAll()
        {
            TestHarness.Reset();
            try
            {
                // 冒烟 / 基本契约
                TestHarness.RunCase("Init_EntersInitialState", Init_EntersInitialState);
                TestHarness.RunCase("ChangeState_ExitsPreviousThenEntersNext", ChangeState_ExitsPreviousThenEntersNext);
                TestHarness.RunCase("Update_AfterInit_DrivesCurrentState", Update_AfterInit_DrivesCurrentState);

                // 边界 / 失败路径
                TestHarness.RunCase("ChangeState_BeforeInit_LogsErrorAndDoesNothing", ChangeState_BeforeInit_LogsErrorAndDoesNothing);
                TestHarness.RunCase("Update_BeforeInit_IsIgnoredWithoutThrowing", Update_BeforeInit_IsIgnoredWithoutThrowing);
                TestHarness.RunCase("Init_Twice_LogsErrorAndKeepsFirstState", Init_Twice_LogsErrorAndKeepsFirstState);
                TestHarness.RunCase("ChangeState_SameState_DoesNotReenter", ChangeState_SameState_DoesNotReenter);

                // 本轮改动：无上限 + 实例复用
                TestHarness.RunCase("ChangeState_OverTenStates_NoEvictionAndNoRebuild", ChangeState_OverTenStates_NoEvictionAndNoRebuild);

                // 集成：状态能通过状态机拿到黑板
                TestHarness.RunCase("State_ReachesBlackBoardThroughMachine", State_ReachesBlackBoardThroughMachine);

                TestHarness.Skip("StateMachine_ThreadSafety",
                    "状态机不做线程安全保证（与框架其余部分一致的主线程约定），无法也不应在此验证并发行为");
                TestHarness.Skip("StateMachine_ExitOnDiscard",
                    "状态机没有 Exit/Clear 入口（机器被丢弃时当前状态的 Exit 不会执行），属未实现能力，不是本轮改动范围");
                TestHarness.Skip("Regression_NoRecordedBugs",
                    "尚无历史缺陷用例；发现缺陷后在此保留具名回归测试");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TEST][FAIL] 套件级异常: {e}");
            }

            TestHarness.Summary("StateMachine");
        }

        // ---------- 冒烟 / 基本契约 ----------

        private static void Init_EntersInitialState()
        {
            StateMachineTestLog.Clear();
            var machine = new StateMachine();

            machine.Init<StateA>();

            TestHarness.Require(StateMachineTestLog.Joined == "New:StateA,Enter:StateA",
                $"Init 应创建并进入初始状态，实际序列：{StateMachineTestLog.Joined}");
        }

        private static void ChangeState_ExitsPreviousThenEntersNext()
        {
            StateMachineTestLog.Clear();
            var machine = new StateMachine();
            machine.Init<StateA>();

            machine.ChangeState<StateB>();

            // 只钉住真正的契约：先退出旧状态、再进入新状态。
            // 不钉"新状态对象何时被创建"——那是实现细节，没有任何设计规定它的先后。
            int exitA = StateMachineTestLog.IndexOf("Exit:StateA");
            int enterB = StateMachineTestLog.IndexOf("Enter:StateB");
            TestHarness.Require(exitA >= 0, $"切换应退出旧状态，实际序列：{StateMachineTestLog.Joined}");
            TestHarness.Require(enterB >= 0, $"切换应进入新状态，实际序列：{StateMachineTestLog.Joined}");
            TestHarness.Require(exitA < enterB,
                $"应先退出旧状态再进入新状态，实际序列：{StateMachineTestLog.Joined}");
            TestHarness.Require(StateMachineTestLog.CountOf("Exit:StateB") == 0,
                $"新状态刚进入不应被退出，实际序列：{StateMachineTestLog.Joined}");
            TestHarness.Require(StateMachineTestLog.CountOf("Enter:StateA") == 1,
                $"初始状态只应进入一次，实际序列：{StateMachineTestLog.Joined}");
        }

        private static void Update_AfterInit_DrivesCurrentState()
        {
            StateMachineTestLog.Clear();
            var machine = new StateMachine();
            machine.Init<StateA>();

            machine.Update();
            machine.ChangeState<StateB>();
            machine.Update();

            TestHarness.Require(StateMachineTestLog.CountOf("Update:StateA") == 1,
                $"Update 应驱动初始状态一次，实际：{StateMachineTestLog.Joined}");
            TestHarness.Require(StateMachineTestLog.CountOf("Update:StateB") == 1,
                $"切换后 Update 应驱动新状态，实际：{StateMachineTestLog.Joined}");
        }

        // ---------- 边界 / 失败路径 ----------

        /// <summary>预期诊断：未 Init 就切换状态打一条报错。</summary>
        private static void ChangeState_BeforeInit_LogsErrorAndDoesNothing()
        {
            StateMachineTestLog.Clear();
            var machine = new StateMachine();

            machine.ChangeState<StateA>(); // 预期诊断

            TestHarness.Require(StateMachineTestLog.Joined.Length == 0,
                $"未初始化时不应进入任何状态，实际序列：{StateMachineTestLog.Joined}");
        }

        /// <summary>本轮修的正是这里的空引用：未 Init 时 Update 必须安静返回。</summary>
        private static void Update_BeforeInit_IsIgnoredWithoutThrowing()
        {
            StateMachineTestLog.Clear();
            var machine = new StateMachine();
            machine.Init<StateA>();

            var fresh = new StateMachine();
            fresh.Update(); // 不得抛异常

            TestHarness.Require(StateMachineTestLog.CountOf("Update:StateA") == 0,
                "未 Init 的机器不应驱动任何状态");
        }

        /// <summary>预期诊断：重复 Init 打一条报错。</summary>
        private static void Init_Twice_LogsErrorAndKeepsFirstState()
        {
            StateMachineTestLog.Clear();
            var machine = new StateMachine();
            machine.Init<StateA>();

            machine.Init<StateB>(); // 预期诊断

            TestHarness.Require(StateMachineTestLog.CountOf("Enter:StateB") == 0,
                $"重复 Init 应被拒绝，不应进入第二个初始状态，实际序列：{StateMachineTestLog.Joined}");
            TestHarness.Require(StateMachineTestLog.CountOf("New:StateB") == 0,
                "重复 Init 被拒绝时连状态都不该创建");
        }

        private static void ChangeState_SameState_DoesNotReenter()
        {
            StateMachineTestLog.Clear();
            var machine = new StateMachine();
            machine.Init<StateA>();
            int enteredBefore = StateMachineTestLog.CountOf("Enter:StateA");

            machine.ChangeState<StateA>();

            TestHarness.Require(StateMachineTestLog.CountOf("Enter:StateA") == enteredBefore,
                $"切到当前状态不应重进，实际序列：{StateMachineTestLog.Joined}");
            TestHarness.Require(StateMachineTestLog.CountOf("Exit:StateA") == 0,
                $"切到当前状态不应 Exit，实际序列：{StateMachineTestLog.Joined}");
        }

        // ---------- 本轮改动：无上限 + 实例复用 ----------

        /// <summary>
        /// 旧实现有"容器上限 10、超出就淘汰"的逻辑，本轮改成无上限。
        /// 走完 11 个状态后再切回第一个：如果还被淘汰，第一个状态会被重建（New:StateA 出现两次）。
        /// </summary>
        private static void ChangeState_OverTenStates_NoEvictionAndNoRebuild()
        {
            StateMachineTestLog.Clear();
            var machine = new StateMachine();
            machine.Init<StateA>();

            machine.ChangeState<StateB>();
            machine.ChangeState<StateC>();
            machine.ChangeState<StateD>();
            machine.ChangeState<StateE>();
            machine.ChangeState<StateF>();
            machine.ChangeState<StateG>();
            machine.ChangeState<StateH>();
            machine.ChangeState<StateI>();
            machine.ChangeState<StateJ>();
            machine.ChangeState<StateK>();
            machine.ChangeState<StateA>();

            TestHarness.Require(StateMachineTestLog.CountOf("New:StateA") == 1,
                $"状态不应因数量被淘汰重建，New:StateA 实际出现 {StateMachineTestLog.CountOf("New:StateA")} 次");
            TestHarness.Require(StateMachineTestLog.CountOf("Enter:StateK") == 1,
                $"第 11 个状态应正常进入（旧实现在此之前就会开始淘汰），实际序列：{StateMachineTestLog.Joined}");
            TestHarness.Require(StateMachineTestLog.CountOf("Exit:StateK") == 1,
                $"切回旧状态时应正常 Exit，实际序列：{StateMachineTestLog.Joined}");
        }

        // ---------- 集成 ----------

        private static void State_ReachesBlackBoardThroughMachine()
        {
            StateMachineTestLog.Clear();
            var machine = new StateMachine();

            machine.Init<BlackBoardProbeState>();

            TestHarness.Require(machine.BlackBoard.GetData<int>("answer") == 42,
                "状态应能在 Enter 里通过状态机写黑板，并在外部读回");
        }
    }
}
