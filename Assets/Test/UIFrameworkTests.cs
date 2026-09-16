using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace ZFrameWork.PlayTests
{
    /// <summary>UI 框架 Play 模式测试套件。</summary>
    public static class UIFrameworkTests
    {
        private const string PathA = "Test/ViewA";
        private const string PathB = "Test/ViewB";
        private const string PathC = "Test/ViewC";
        private const string PathPopupA = "Test/PopupA";
        private const string PathPopupB = "Test/PopupB";

        // 工厂每次为管理器/面板创建独立的 FakeResourceGroup（真实工厂每次 new，同样语义。
        // 不能共享一个实例——面板 CloseInternal 会冷却自己的资源组，共享会连带冷却管理器的）。
        // 失败开关与延迟配置用 FakeResourceGroup 的静态字段，保证长寿的管理器组也能被当前用例控制。
        private static readonly Dictionary<string, Object> PrefabAssets = new();
        private static readonly List<FakeResourceGroup> CreatedGroups = new();

        private static FakeResourceGroup CreateFakeGroup()
        {
            FakeResourceGroup group = new FakeResourceGroup();
            foreach (KeyValuePair<string, Object> kv in PrefabAssets)
                group.RegisterPrefab(kv.Key, kv.Value);
            CreatedGroups.Add(group);
            return group;
        }

        private static readonly List<Object> PrefabSources = new();
        private static bool _initialized;

        public static async Task RunAll()
        {
            TestHarness.Reset();
            try
            {
                SetupOnce();

                // 冒烟 / 基本契约
                await TestHarness.RunCaseAsync("Open_NewView_LoadsHiddenStaysInactive",
                    Open_NewView_LoadsHiddenStaysInactive);
                await TestHarness.RunCaseAsync("Show_AfterOpen_ActivatesAndCallsOnShow",
                    Show_AfterOpen_ActivatesAndCallsOnShow);
                await TestHarness.RunCaseAsync("Show_WithoutOpen_OpensThenShows",
                    Show_WithoutOpen_OpensThenShows);
                await TestHarness.RunCaseAsync("Close_Top_ShowsPreviousInterface",
                    Close_Top_ShowsPreviousInterface);
                await TestHarness.RunCaseAsync("Close_LastInterface_RemovesWithoutError",
                    Close_LastInterface_RemovesWithoutError);
                await TestHarness.RunCaseAsync("Show_MiddleView_ClosesAboveOnly",
                    Show_MiddleView_ClosesAboveOnly);
                await TestHarness.RunCaseAsync("Close_MiddleNode_KeepsRemainingOrder",
                    Close_MiddleNode_KeepsRemainingOrder);
                await TestHarness.RunCaseAsync("Hide_OnlyHidesKeepsStackIntact",
                    Hide_OnlyHidesKeepsStackIntact);
                // 回归：SetNode 重构曾使 Hide 未打开界面时凭空创建节点并触发加载
                await TestHarness.RunCaseAsync("Hide_MissingView_NoSideEffect",
                    Hide_MissingView_NoSideEffect);

                // 生命周期
                await TestHarness.RunCaseAsync("Lifecycle_OnCallsFollowDocumentedOrder",
                    Lifecycle_OnCallsFollowDocumentedOrder);
                await TestHarness.RunCaseAsync("Lifecycle_DoubleClose_IsSafe",
                    Lifecycle_DoubleClose_IsSafe);
                await TestHarness.RunCaseAsync("Lifecycle_ReopenAfterClose_WorksFresh",
                    Lifecycle_ReopenAfterClose_WorksFresh);

                // 变更 / 竞态
                await TestHarness.RunCaseAsync("Race_CloseDuringLoad_VoidsLateResult",
                    Race_CloseDuringLoad_VoidsLateResult);
                await TestHarness.RunCaseAsync("Race_DuplicateOpen_SingleLoadOnly",
                    Race_DuplicateOpen_SingleLoadOnly);
                await TestHarness.RunCaseAsync("Race_OutOfOrderCompletion_TargetStatesWin",
                    Race_OutOfOrderCompletion_TargetStatesWin);

                // 异常 / 失败路径（预期触发的 ZLog 报错属于契约的一部分）
                await TestHarness.RunCaseAsync("Failure_UnregisteredPath_RemovesNode",
                    Failure_UnregisteredPath_RemovesNode);
                await TestHarness.RunCaseAsync("Failure_LoadFails_RemovesNodeAndRecovers",
                    Failure_LoadFails_RemovesNodeAndRecovers);
                await TestHarness.RunCaseAsync("Failure_PrefabMissingViewComponent_RemovesNode",
                    Failure_PrefabMissingViewComponent_RemovesNode);

                // 弹窗
                await TestHarness.RunCaseAsync("Popup_Push_DisplaysWithDataInjectedBeforeOnShow",
                    Popup_Push_DisplaysWithDataInjectedBeforeOnShow);
                await TestHarness.RunCaseAsync("Popup_DuplicatePush_Rejected",
                    Popup_DuplicatePush_Rejected);
                await TestHarness.RunCaseAsync("Popup_PopView_ClosesTopOnly",
                    Popup_PopView_ClosesTopOnly);
                await TestHarness.RunCaseAsync("Popup_Clear_ClosesAll",
                    Popup_Clear_ClosesAll);
                await TestHarness.RunCaseAsync("Popup_Blocker_SitsBelowTopHiddenWhenEmpty",
                    Popup_Blocker_SitsBelowTopHiddenWhenEmpty);
                await TestHarness.RunCaseAsync("Popup_SwitchByOpen_ClearsPopups",
                    Popup_SwitchByOpen_ClearsPopups);
                await TestHarness.RunCaseAsync("Popup_SwitchByCloseTop_ClearsPopups",
                    Popup_SwitchByCloseTop_ClearsPopups);

                // 集成
                await TestHarness.RunCaseAsync("Integration_UIRoot_CreatesLayersAndEventSystem",
                    Integration_UIRoot_CreatesLayersAndEventSystem);
                await TestHarness.RunCaseAsync("Integration_Close_ReleasesPrefabResource",
                    Integration_Close_ReleasesPrefabResource);

                // 压力 / 重复
                await TestHarness.RunCaseAsync("Stress_RepeatedOpenShowClose_EndsClean",
                    Stress_RepeatedOpenShowClose_EndsClean);
                await TestHarness.RunCaseAsync("Stress_PopupPushPopCycles_EndsClean",
                    Stress_PopupPushPopCycles_EndsClean);

                // 回归：暂无历史缺陷记录，占位说明
                TestHarness.Skip("Regression_NoRecordedBugs", "尚无历史缺陷用例；发现缺陷后在此保留具名回归测试");
            }
            catch (OperationCanceledException)
            {
                _aborted = true;
                Debug.Log("[TEST][SUITE] 检测到 Play 模式已退出，剩余用例中止。");
            }
            finally
            {
                TearDown();
            }

            if (TestHarness.Failed > 0 || _aborted)
                Debug.Log("[TEST][SUITE] 套件存在失败或被中止，未给出通过结论。");
            TestHarness.Summary("UIFramework");
        }

        private static bool _aborted;

        // ---------- Setup / Teardown ----------

        private static void SetupOnce()
        {
            if (_initialized)
                return;
            _initialized = true;

            ResourceGroupFactory.SetCreator(CreateFakeGroup);
            _ = UIRoot.Instance; // 触发 Awake：创建 EventSystem 与各层管理器

            RegisterView<TestViewA>(PathA);
            RegisterView<TestViewB>(PathB);
            RegisterView<TestViewC>(PathC);
            RegisterPopup();
        }

        private static void RegisterView<T>(string path) where T : Component
        {
            GameObject go = new GameObject($"Prefab_{typeof(T).Name}");
            go.AddComponent<T>();
            PrefabSources.Add(go);
            PrefabAssets[path] = go;
            UIViewConfig.Paths[typeof(T)] = path;
        }

        private static void RegisterPopup()
        {
            GameObject popupA = new GameObject("Prefab_TestPopupA");
            popupA.AddComponent<TestPopupA>();
            PrefabSources.Add(popupA);
            PrefabAssets[PathPopupA] = popupA;
            UIViewConfig.Paths[typeof(TestPopupA)] = PathPopupA;

            GameObject popupB = new GameObject("Prefab_TestPopupB");
            popupB.AddComponent<TestPopupB>();
            PrefabSources.Add(popupB);
            PrefabAssets[PathPopupB] = popupB;
            UIViewConfig.Paths[typeof(TestPopupB)] = PathPopupB;
        }

        private static void TearDown()
        {
            // Play 模式退出后访问单例会触发重建并在 DontDestroyOnLoad 处抛错，直接放弃清理
            if (!Application.isPlaying)
                return;

            SingletonUIManager.Instance?.Clear();
            PopupManager.Instance?.Clear();
            foreach (Object source in PrefabSources)
            {
                if (source != null)
                    Object.Destroy(source);
            }
            PrefabSources.Clear();
        }

        /// <summary>每个用例前的状态复位：清空两个管理器并还原替身配置。</summary>
        private static async Task ResetState()
        {
            // Play 模式退出后 UnitySynchronizationContext 仍会泵剩余延续，
            // 此时一切 Unity 状态均不可信，直接中止整个套件
            if (!Application.isPlaying)
                throw new OperationCanceledException("Play 模式已退出，中止测试套件");

            SingletonUIManager.Instance?.Clear();
            PopupManager.Instance?.Clear();
            FakeResourceGroup.FailAllLoadsConfig = false;
            FakeResourceGroup.DelayConfig.Clear();
            await WaitForNoInstances();
        }

        private static int CountInstances<T>() where T : Object
        {
            T[] found = Object.FindObjectsOfType<T>(false);
            return found.Length;
        }

        private static int CountViewInstancesExcludingPrefabs()
        {
            CountingView[] found = Object.FindObjectsOfType<CountingView>(true);
            int count = 0;
            foreach (CountingView view in found)
            {
                if (view == null)
                    continue;
                bool isPrefabSource = false;
                foreach (Object source in PrefabSources)
                {
                    // 组件与源 GameObject 需按物体比较（Unity 重载 == 兼容已销毁对象）
                    if (view.gameObject == source)
                    {
                        isPrefabSource = true;
                        break;
                    }
                }
                if (!isPrefabSource)
                    count++;
            }
            return count;
        }

        private static async Task WaitForNoInstances()
        {
            await TestHarness.WaitFor(() => CountViewInstancesExcludingPrefabs() == 0,
                3000, "等待上一用例实例销毁完毕");
        }

        private static UINode AwaitLoaded(System.Type key)
        {
            UINode node = SingletonUIManager.Instance.Find(key);
            TestHarness.Require(node != null, $"界面 {key.Name} 应已入栈");
            return node;
        }

        // ---------- 冒烟 / 基本契约 ----------

        private static async Task Open_NewView_LoadsHiddenStaysInactive()
        {
            await ResetState();
            SingletonUIManager.Instance.Open<TestViewA>();

            UINode node = AwaitLoaded(typeof(TestViewA));
            await TestHarness.WaitFor(() => node != null && node.UI != null, 2000, "TestViewA 加载完成");
            TestHarness.Require(node.UI.IsOpen, "Open 后界面应为已打开状态");
            TestHarness.Require(!node.UI.IsShown, "Open 后界面不应显示");
            TestHarness.Require(!node.UI.gameObject.activeSelf, "Open 后物体应保持未激活");
            TestHarness.Require(node.UI.Key == PathA, "界面 Key 应为加载路径");
        }

        private static async Task Show_AfterOpen_ActivatesAndCallsOnShow()
        {
            await ResetState();
            var manager = SingletonUIManager.Instance;
            manager.Open<TestViewA>();
            UINode node = manager.Find(typeof(TestViewA));
            await TestHarness.WaitFor(() => node != null && node.UI != null, 2000, "加载完成");

            manager.Show<TestViewA>();
            TestHarness.Require(node.UI.IsShown, "Show 后界面应显示");
            TestHarness.Require(node.UI.gameObject.activeSelf, "Show 后物体应激活");
            TestHarness.Require(((TestViewA)node.UI).Calls.Contains("Show"), "应调用过 OnShow");
        }

        private static async Task Show_WithoutOpen_OpensThenShows()
        {
            await ResetState();
            SingletonUIManager.Instance.Show<TestViewA>();

            UINode node = SingletonUIManager.Instance.Find(typeof(TestViewA));
            TestHarness.Require(node != null, "Show 未 Open 的界面应先入栈");
            await TestHarness.WaitFor(() => node != null && node.UI != null, 2000, "加载完成");
            await TestHarness.WaitFor(() => node != null && node.UI.IsShown, 2000, "加载完成后应按目标状态显示");
        }

        private static async Task Close_Top_ShowsPreviousInterface()
        {
            await ResetState();
            var manager = SingletonUIManager.Instance;
            manager.Show<TestViewA>();
            manager.Show<TestViewB>(); // 出栈到 B：A 关闭？A 在 B 之下……先等 A 加载完成再 Show B，保证栈序 [A,B]
            UINode nodeA = manager.Find(typeof(TestViewA));
            await TestHarness.WaitFor(() => nodeA?.UI != null, 2000, "A 加载");
            manager.Show<TestViewB>();
            UINode nodeB = manager.Find(typeof(TestViewB));
            await TestHarness.WaitFor(() => nodeB?.UI != null, 2000, "B 加载");
            await TestHarness.WaitFor(() => nodeB != null && nodeB.UI.IsShown, 2000, "B 显示");

            manager.Close<TestViewB>();
            TestHarness.Require(manager.Find(typeof(TestViewB)) == null, "B 应已移除");
            TestHarness.Require(nodeA.UI != null && nodeA.UI.IsShown, "关闭栈顶后应显示上一个界面 A");
        }

        private static async Task Close_LastInterface_RemovesWithoutError()
        {
            await ResetState();
            var manager = SingletonUIManager.Instance;
            manager.Show<TestViewA>();
            UINode node = manager.Find(typeof(TestViewA));
            await TestHarness.WaitFor(() => node != null && node.UI != null, 2000, "加载完成");

            manager.Close<TestViewA>();
            TestHarness.Require(manager.Find(typeof(TestViewA)) == null, "最后一个界面 Close 应直接移除");
        }

        private static async Task Show_MiddleView_ClosesAboveOnly()
        {
            await ResetState();
            var manager = SingletonUIManager.Instance;
            manager.Show<TestViewA>();
            UINode a = manager.Find(typeof(TestViewA));
            await TestHarness.WaitFor(() => a != null && a.UI != null, 2000, "A 加载");
            manager.Open<TestViewB>();
            UINode b = manager.Find(typeof(TestViewB));
            await TestHarness.WaitFor(() => b != null && b.UI != null, 2000, "B 加载");
            manager.Open<TestViewC>();
            UINode c = manager.Find(typeof(TestViewC));
            await TestHarness.WaitFor(() => c != null && c.UI != null, 2000, "C 加载");

            manager.Show<TestViewA>(); // 回退到 A：B、C 出栈关闭
            TestHarness.Require(manager.Find(typeof(TestViewB)) == null, "Show 中间界面应关闭其上的 B");
            TestHarness.Require(manager.Find(typeof(TestViewC)) == null, "Show 中间界面应关闭其上的 C");
            TestHarness.Require(a.UI != null && a.UI.IsShown, "A 应显示");
        }

        private static async Task Close_MiddleNode_KeepsRemainingOrder()
        {
            await ResetState();
            var manager = SingletonUIManager.Instance;
            manager.Show<TestViewA>();
            UINode a = manager.Find(typeof(TestViewA));
            await TestHarness.WaitFor(() => a != null && a.UI != null, 2000, "A 加载");
            manager.Open<TestViewB>();
            await TestHarness.WaitFor(() => manager.Find(typeof(TestViewB))?.UI != null, 2000, "B 加载");
            manager.Open<TestViewC>();
            UINode c = manager.Find(typeof(TestViewC));
            await TestHarness.WaitFor(() => c != null && c.UI != null, 2000, "C 加载");

            manager.Close<TestViewB>(); // 中间移除
            TestHarness.Require(manager.Find(typeof(TestViewB)) == null, "B 应从中间移除");
            TestHarness.Require(manager.Find(typeof(TestViewA)) != null, "A 应保留");
            TestHarness.Require(manager.Find(typeof(TestViewC)) != null, "C 应保留");

            manager.Show<TestViewC>(); // C 仍是栈顶，直接显示，A 不受影响
            await TestHarness.WaitFor(() => c != null && c.UI.IsShown, 2000, "C 显示");
            TestHarness.Require(manager.Find(typeof(TestViewA)) != null, "顺序未被扰动，A 仍在栈中");
        }

        private static async Task Hide_OnlyHidesKeepsStackIntact()
        {
            await ResetState();
            var manager = SingletonUIManager.Instance;
            manager.Show<TestViewA>();
            UINode node = manager.Find(typeof(TestViewA));
            await TestHarness.WaitFor(() => node != null && node.UI != null, 2000, "加载完成");
            await TestHarness.WaitFor(() => node != null && node.UI.IsShown, 2000, "显示");

            manager.Hide<TestViewA>();
            TestHarness.Require(!node.UI.IsShown, "Hide 后不应显示");
            TestHarness.Require(node.UI.IsOpen, "Hide 后界面应仍处于打开状态");
            TestHarness.Require(manager.Find(typeof(TestViewA)) != null, "Hide 不应改变栈");

            manager.Show<TestViewA>();
            TestHarness.Require(node.UI.IsShown, "Hide 后可重新 Show");
        }

        private static async Task Hide_MissingView_NoSideEffect()
        {
            await ResetState();
            var manager = SingletonUIManager.Instance;
            manager.Hide<TestViewA>();   // 界面从未打开

            await Task.Delay(100);       // 给可能误触发的同步加载留出窗口
            TestHarness.Require(manager.Find(typeof(TestViewA)) == null,
                "Hide 未打开的界面不应创建节点，更不应触发加载");
            TestHarness.Require(CountViewInstancesExcludingPrefabs() == 0,
                "Hide 未打开的界面不应产生任何实例");
        }

        // ---------- 生命周期 ----------

        private static async Task Lifecycle_OnCallsFollowDocumentedOrder()
        {
            await ResetState();
            var manager = SingletonUIManager.Instance;
            manager.Show<TestViewA>();
            UINode node = manager.Find(typeof(TestViewA));
            await TestHarness.WaitFor(() => node != null && node.UI != null, 2000, "加载完成");
            var view = (TestViewA)node.UI;   // Close 会把 node.UI 置空，先保存引用
            manager.Hide<TestViewA>();
            manager.Close<TestViewA>();

            TestHarness.Require(view != null && view.Calls.Count == 4, $"应恰好 4 次生命周期调用，实际 {(view != null ? view.Calls.Count : -1)}");
            TestHarness.Require(view.Calls[0] == "Open" && view.Calls[1] == "Show"
                && view.Calls[2] == "Hide" && view.Calls[3] == "Close",
                $"调用顺序应为 Open,Show,Hide,Close，实际 {string.Join(",", view.Calls)}");
            TestHarness.Require(view.ActiveSelfDuringOnHide == true, "OnHide 应在物体失活之前调用");
        }

        private static async Task Lifecycle_DoubleClose_IsSafe()
        {
            await ResetState();
            var manager = SingletonUIManager.Instance;
            manager.Show<TestViewA>();
            UINode node = manager.Find(typeof(TestViewA));
            await TestHarness.WaitFor(() => node != null && node.UI != null, 2000, "加载完成");

            manager.Close<TestViewA>();
            manager.Close<TestViewA>(); // 第二次不应抛异常
            TestHarness.Require(manager.Find(typeof(TestViewA)) == null, "重复 Close 后节点应已移除");
        }

        private static async Task Lifecycle_ReopenAfterClose_WorksFresh()
        {
            await ResetState();
            var manager = SingletonUIManager.Instance;
            manager.Show<TestViewA>();
            UINode first = manager.Find(typeof(TestViewA));
            await TestHarness.WaitFor(() => first != null && first.UI != null, 2000, "首次加载");
            manager.Close<TestViewA>();
            await WaitForNoInstances();

            manager.Show<TestViewA>();
            UINode second = manager.Find(typeof(TestViewA));
            TestHarness.Require(second != first, "重开应产生新节点");
            await TestHarness.WaitFor(() => second != null && second.UI != null && second.UI.IsShown, 2000, "重开后显示");
            TestHarness.Require(((TestViewA)second.UI).Calls.Count == 2, "新实例应重新走 Open,Show");
        }

        // ---------- 变更 / 竞态 ----------

        private static async Task Race_CloseDuringLoad_VoidsLateResult()
        {
            await ResetState();
            FakeResourceGroup.DelayConfig[PathA] = 150;
            var manager = SingletonUIManager.Instance;
            manager.Open<TestViewA>();
            manager.Close<TestViewA>(); // 加载在途时立即关闭

            await TestHarness.WaitFor(() => manager.Find(typeof(TestViewA)) == null, 500, "节点应立即移除");
            await Task.Delay(400); // 等迟到的加载完成
            await TestHarness.WaitFor(() => CountViewInstancesExcludingPrefabs() == 0,
                2000, "迟到结果应作废，不产生实例");
        }

        private static async Task Race_DuplicateOpen_SingleLoadOnly()
        {
            await ResetState();
            var manager = SingletonUIManager.Instance;
            bool first = manager.Open<TestViewA>();
            bool second = manager.Open<TestViewA>(); // 重复请求：语义保持，不重复加载
            TestHarness.Require(first && second, "两次 Open 都应受理");
            UINode node = manager.Find(typeof(TestViewA));
            await TestHarness.WaitFor(() => node != null && node.UI != null, 2000, "加载完成");
            await TestHarness.WaitFor(() => CountViewInstancesExcludingPrefabs() == 1,
                2000, "应只有一个实例");
            TestHarness.Require(((TestViewA)node.UI).Calls.Count == 1, "OnOpen 只应调用一次");
        }

        private static async Task Race_OutOfOrderCompletion_TargetStatesWin()
        {
            await ResetState();
            FakeResourceGroup.DelayConfig[PathA] = 250; // A 慢
            var manager = SingletonUIManager.Instance;
            manager.Open<TestViewA>();      // A 目标：存在且隐藏
            manager.Show<TestViewB>();      // B 快，目标：显示

            UINode b = manager.Find(typeof(TestViewB));
            await TestHarness.WaitFor(() => b != null && b.UI != null && b.UI.IsShown, 2000, "B 先完成并显示");

            UINode a = manager.Find(typeof(TestViewA));
            await TestHarness.WaitFor(() => a != null && a.UI != null, 2000, "A 后完成");
            TestHarness.Require(a.UI.IsOpen && !a.UI.IsShown, "A 晚到也应按目标状态保持隐藏入栈");
            TestHarness.Require(b.UI.IsShown, "B 的显示不应被 A 的迟到影响");

            manager.Show<TestViewA>(); // 回退到 A
            TestHarness.Require(manager.Find(typeof(TestViewB)) == null, "B 应出栈关闭");
            TestHarness.Require(a.UI.IsShown, "A 应显示");
        }

        // ---------- 异常 / 失败路径 ----------

        private static async Task Failure_UnregisteredPath_RemovesNode()
        {
            await ResetState();
            // TestViewB 已注册，先注销模拟未注册：用未注册的新类型最直接——改用反注册不可行，
            // 这里用 LoadFails 之外的场景：直接用一个未注册路径的视图类型。
            var manager = SingletonUIManager.Instance;
            manager.Open<UnregisteredView>(); // 预期触发 ZLog 报错并删除节点
            await Task.Delay(100);
            TestHarness.Require(manager.Find(typeof(UnregisteredView)) == null,
                "路径未注册时应报错并删除节点");
        }

        private static async Task Failure_LoadFails_RemovesNodeAndRecovers()
        {
            await ResetState();
            FakeResourceGroup.FailAllLoadsConfig = true;
            var manager = SingletonUIManager.Instance;
            manager.Open<TestViewA>(); // 预期触发 ZLog 报错
            await TestHarness.WaitFor(() => manager.Find(typeof(TestViewA)) == null,
                2000, "加载失败后节点应删除");

            FakeResourceGroup.FailAllLoadsConfig = false;
            manager.Show<TestViewA>(); // 恢复后可正常打开
            UINode node = manager.Find(typeof(TestViewA));
            await TestHarness.WaitFor(() => node != null && node.UI != null && node.UI.IsShown, 2000, "恢复后正常加载显示");
        }

        private static async Task Failure_PrefabMissingViewComponent_RemovesNode()
        {
            await ResetState();
            // 注册一个不含目标组件的预制体到新类型路径
            GameObject plain = new GameObject("Prefab_PlainNoView");
            PrefabSources.Add(plain);
            PrefabAssets["Test/Missing"] = plain;
            UIViewConfig.Paths[typeof(MissingComponentView)] = "Test/Missing";

            var manager = SingletonUIManager.Instance;
            manager.Open<MissingComponentView>(); // 预期触发 ZLog 报错并删除节点
            await TestHarness.WaitFor(() => manager.Find(typeof(MissingComponentView)) == null,
                2000, "预制体缺组件时应删除节点");
        }

        // ---------- 弹窗 ----------

        private static async Task Popup_Push_DisplaysWithDataInjectedBeforeOnShow()
        {
            await ResetState();
            bool accepted = PopupManager.Instance.Push<TestPopupA, TestPopupA.Data>(new TestPopupA.Data(42, "answer"));
            TestHarness.Require(accepted, "首次 Push 应受理");

            UINode node = PopupManager.Instance.Find(typeof(TestPopupA));
            await TestHarness.WaitFor(() => node != null && node.UI != null && node.UI.IsShown, 2000, "弹窗加载并显示");
            var popup = (TestPopupA)node.UI;
            TestHarness.Require(popup.ValueSeenOnShow == 42, "OnShow 时数据应已注入");
            TestHarness.Require(((PopupBase<TestPopupA.Data>)popup).Data.Label == "answer", "Data 字段应完整传递");
        }

        private static async Task Popup_DuplicatePush_Rejected()
        {
            await ResetState();
            var manager = PopupManager.Instance;
            TestHarness.Require(manager.Push<TestPopupA, TestPopupA.Data>(new TestPopupA.Data(1, "")),
                "首次 Push 应受理");
            bool rejected = manager.Push<TestPopupA, TestPopupA.Data>(new TestPopupA.Data(2, ""));
            TestHarness.Require(!rejected, "加载中的重复 Push 应被拒绝");

            UINode node = manager.Find(typeof(TestPopupA));
            await TestHarness.WaitFor(() => node != null && node.UI != null, 2000, "加载完成");
            rejected = manager.Push<TestPopupA, TestPopupA.Data>(new TestPopupA.Data(3, ""));
            TestHarness.Require(!rejected, "已显示的重复 Push 应被拒绝");
        }

        private static async Task Popup_PopView_ClosesTopOnly()
        {
            await ResetState();
            var manager = PopupManager.Instance;
            manager.Push<TestPopupA, TestPopupA.Data>(new TestPopupA.Data(1, ""));
            UINode a = manager.Find(typeof(TestPopupA));
            await TestHarness.WaitFor(() => a != null && a.UI != null, 2000, "A 加载");
            manager.Push<TestPopupB, TestPopupB.Data>(new TestPopupB.Data(true));
            UINode b = manager.Find(typeof(TestPopupB));
            await TestHarness.WaitFor(() => b != null && b.UI != null, 2000, "B 加载");

            manager.PopView();
            TestHarness.Require(manager.Find(typeof(TestPopupB)) == null, "PopView 应只关闭栈顶 B");
            TestHarness.Require(manager.Find(typeof(TestPopupA)) != null && a.UI.IsShown, "A 应保留并显示");
        }

        private static async Task Popup_Clear_ClosesAll()
        {
            await ResetState();
            var manager = PopupManager.Instance;
            manager.Push<TestPopupA, TestPopupA.Data>(new TestPopupA.Data(1, ""));
            manager.Push<TestPopupB, TestPopupB.Data>(new TestPopupB.Data(true));
            UINode b = manager.Find(typeof(TestPopupB));
            await TestHarness.WaitFor(() => b != null && b.UI != null, 2000, "B 加载");

            manager.Clear();
            TestHarness.Require(manager.Find(typeof(TestPopupA)) == null, "Clear 后 A 应移除");
            TestHarness.Require(manager.Find(typeof(TestPopupB)) == null, "Clear 后 B 应移除");
        }

        private static async Task Popup_Blocker_SitsBelowTopHiddenWhenEmpty()
        {
            await ResetState();
            var manager = PopupManager.Instance;
            manager.Push<TestPopupA, TestPopupA.Data>(new TestPopupA.Data(1, ""));
            UINode a = manager.Find(typeof(TestPopupA));
            await TestHarness.WaitFor(() => a != null && a.UI != null, 2000, "A 加载");
            manager.Push<TestPopupB, TestPopupB.Data>(new TestPopupB.Data(true));
            UINode b = manager.Find(typeof(TestPopupB));
            await TestHarness.WaitFor(() => b != null && b.UI != null, 2000, "B 加载");

            UIBlocker blocker = Object.FindObjectsOfType<UIBlocker>(true)[0];
            TestHarness.Require(blocker.gameObject.activeSelf, "有弹窗时拦截器应激活");
            TestHarness.Require(blocker.transform.GetSiblingIndex() == 1,
                $"双弹窗时拦截器兄弟索引应为 1（n-1），实际 {blocker.transform.GetSiblingIndex()}");
            TestHarness.Require(b.UI.transform.GetSiblingIndex() == 2, "栈顶弹窗应在拦截器之上");

            manager.PopView();
            TestHarness.Require(blocker.transform.GetSiblingIndex() == 0,
                $"单弹窗时拦截器兄弟索引应为 0，实际 {blocker.transform.GetSiblingIndex()}");

            manager.Clear();
            TestHarness.Require(!blocker.gameObject.activeSelf, "栈空时拦截器应隐藏");
        }

        private static async Task Popup_SwitchByOpen_ClearsPopups()
        {
            await ResetState();
            var popups = PopupManager.Instance;
            popups.Push<TestPopupA, TestPopupA.Data>(new TestPopupA.Data(1, ""));
            UINode popup = popups.Find(typeof(TestPopupA));
            await TestHarness.WaitFor(() => popup != null && popup.UI != null, 2000, "弹窗加载");

            SingletonUIManager.Instance.Open<TestViewC>(); // 新界面 Open 一开始应清空弹窗
            TestHarness.Require(popups.Find(typeof(TestPopupA)) == null, "界面切换 Open 应清空弹窗");
        }

        private static async Task Popup_SwitchByCloseTop_ClearsPopups()
        {
            await ResetState();
            var manager = SingletonUIManager.Instance;
            manager.Show<TestViewA>();
            UINode view = manager.Find(typeof(TestViewA));
            await TestHarness.WaitFor(() => view != null && view.UI != null, 2000, "界面加载");

            var popups = PopupManager.Instance;
            popups.Push<TestPopupA, TestPopupA.Data>(new TestPopupA.Data(1, ""));
            UINode popup = popups.Find(typeof(TestPopupA));
            await TestHarness.WaitFor(() => popup != null && popup.UI != null, 2000, "弹窗加载");

            manager.Close<TestViewA>(); // 关闭当前界面 = 切换，应清空弹窗
            TestHarness.Require(popups.Find(typeof(TestPopupA)) == null, "关闭栈顶界面应清空弹窗");
        }

        // ---------- 集成 ----------

        private static Task Integration_UIRoot_CreatesLayersAndEventSystem()
        {
            TestHarness.Require(UIRoot.Instance != null, "UIRoot 应存在");
            TestHarness.Require(SingletonUIManager.Instance != null, "单例 UI 管理器应已创建");
            TestHarness.Require(PopupManager.Instance != null, "弹窗管理器应已创建");

            Canvas[] canvases = Object.FindObjectsOfType<Canvas>(false);
            int found = 0;
            List<string> canvasNames = new List<string>();
            foreach (Canvas canvas in canvases)
            {
                canvasNames.Add($"{canvas.name}({canvas.sortingOrder})");
                if (canvas.name == "SingletonUIManagerCanvas")
                {
                    TestHarness.Require(canvas.sortingOrder == 0, "单例层 Canvas 排序应为 0");
                    found++;
                }
                else if (canvas.name == "PopupManagerCanvas")
                {
                    TestHarness.Require(canvas.sortingOrder == 100, "弹窗层 Canvas 排序应为 100");
                    found++;
                }
            }
            // 场景里可能残留历史运行时 Canvas（误存进场景文件），只要求我们的两层存在且排序正确
            TestHarness.Require(found >= 2,
                $"应至少各创建一个层级 Canvas，实际 {found}，场景全部 Canvas：{string.Join(", ", canvasNames)}");
            TestHarness.Require(Object.FindObjectsOfType<EventSystem>(false).Length >= 1, "EventSystem 应存在");
            return Task.CompletedTask;
        }

        private static async Task Integration_Close_ReleasesPrefabResource()
        {
            await ResetState();
            var manager = SingletonUIManager.Instance;
            manager.Show<TestViewA>();
            UINode node = manager.Find(typeof(TestViewA));
            await TestHarness.WaitFor(() => node != null && node.UI != null, 2000, "加载完成");

            manager.Close<TestViewA>();
            bool released = false;
            foreach (FakeResourceGroup group in CreatedGroups)
            {
                if (group.IsReleased(PathA))
                    released = true;
            }
            TestHarness.Require(released, "关闭界面应释放预制体资源引用");
        }

        // ---------- 压力 ----------

        private static async Task Stress_RepeatedOpenShowClose_EndsClean()
        {
            await ResetState();
            var manager = SingletonUIManager.Instance;
            var viewTypes = new[] { typeof(TestViewA), typeof(TestViewB), typeof(TestViewC) };

            const int rounds = 40;
            for (int i = 0; i < rounds; i++)
            {
                System.Type type = viewTypes[i % viewTypes.Length];
                manager.Show(type);
                UINode node = manager.Find(type);
                await TestHarness.WaitFor(() => node != null && node.UI != null, 2000, $"第 {i} 轮加载");
                await TestHarness.WaitFor(() => node != null && node.UI.IsShown, 2000, $"第 {i} 轮显示");
                manager.Close(type);
            }

            await TestHarness.WaitFor(() =>
                manager.Find(typeof(TestViewA)) == null
                && manager.Find(typeof(TestViewB)) == null
                && manager.Find(typeof(TestViewC)) == null,
                2000, "压力后栈应清空");
            await TestHarness.WaitFor(() => CountViewInstancesExcludingPrefabs() == 0,
                3000, "压力后不应残留实例");
        }

        private static async Task Stress_PopupPushPopCycles_EndsClean()
        {
            await ResetState();
            var manager = PopupManager.Instance;

            const int cycles = 30;
            for (int i = 0; i < cycles; i++)
            {
                TestHarness.Require(
                    manager.Push<TestPopupA, TestPopupA.Data>(new TestPopupA.Data(i, "")),
                    $"第 {i} 次 Push 应受理");
                UINode node = manager.Find(typeof(TestPopupA));
                await TestHarness.WaitFor(() => node != null && node.UI != null && node.UI.IsShown, 2000, $"第 {i} 次显示");
                manager.PopView();
                await TestHarness.WaitFor(() => manager.Find(typeof(TestPopupA)) == null,
                    2000, $"第 {i} 次关闭");
            }

            TestHarness.Require(!GetBlocker().gameObject.activeSelf, "压力后拦截器应隐藏");
        }

        private static UIBlocker GetBlocker()
        {
            // includeInactive：栈空时拦截器是隐藏的，按 false 查会拿到空数组
            return Object.FindObjectsOfType<UIBlocker>(true)[0];
        }

        // ---------- 测试专用视图类型（未注册路径 / 缺组件场景） ----------

        /// <summary>从未在 UIViewConfig 注册路径的类型。</summary>
        public sealed class UnregisteredView : CountingView { }

        /// <summary>注册的预制体不含该组件的类型。</summary>
        public sealed class MissingComponentView : CountingView { }
    }
}
