using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ZFrameWork.PlayTests
{
    /// <summary>
    /// 分层 Canvas 的 Play 模式测试套件。对应设计文档第 4 条（分层管理）与第 10、12 条（UIRoot 不灭、创建 EventSystem）：
    /// UIRoot 按 UIManagerConfig 逐层创建 Canvas 与管理器，Canvas 设为"随屏幕大小缩放 + Expand"。
    /// </summary>
    public static class CanvasTests
    {
        private const string PathView = "Test/View";
        private const string PathPopup = "Test/Popup";

        private static readonly Dictionary<string, Object> PrefabAssets = new();
        private static readonly List<Object> PrefabSources = new();
        private static bool _initialized;
        private static bool _aborted;

        public static async Task RunAll()
        {
            TestHarness.Reset();
            try
            {
                SetupOnce();

                // 冒烟 / 基本契约
                await TestHarness.RunCaseAsync("Canvas_Layers_OneCanvasPerLayerInOrder",
                    Canvas_Layers_OneCanvasPerLayerInOrder);
                // 缩放配置
                await TestHarness.RunCaseAsync("CanvasScaler_UiScaleModeIsScaleWithScreenSize",
                    CanvasScaler_UiScaleModeIsScaleWithScreenSize);
                await TestHarness.RunCaseAsync("CanvasScaler_ScreenMatchModeIsExpand",
                    CanvasScaler_ScreenMatchModeIsExpand);
                await TestHarness.RunCaseAsync("CanvasScaler_Expand_KeepsDesignAreaVisible",
                    CanvasScaler_Expand_KeepsDesignAreaVisible);
                // 生命周期
                await TestHarness.RunCaseAsync("UIRoot_RepeatedAccess_NoExtraLayers",
                    UIRoot_RepeatedAccess_NoExtraLayers);
                await TestHarness.RunCaseAsync("UIRoot_CleanupForSceneSwitch_ClearsViewsKeepsLayers",
                    UIRoot_CleanupForSceneSwitch_ClearsViewsKeepsLayers);
                await TestHarness.RunCaseAsync("UIRoot_DontDestroyOnLoadSingleton_CreatesEventSystem",
                    UIRoot_DontDestroyOnLoadSingleton_CreatesEventSystem);
                // 集成：界面真的落到对应层的 Canvas 里
                await TestHarness.RunCaseAsync("Integration_ViewAndPopup_GoToTheirOwnLayerCanvas",
                    Integration_ViewAndPopup_GoToTheirOwnLayerCanvas);

                // 以下类别本目标无法覆盖，显式标记而不是留空
                TestHarness.Skip("Canvas_InvalidManagerType_LogsError",
                    "UIManagerConfig.ManagerTypes 是 static readonly 数组，测试无法注入非 UIManager 的类型来触发报错分支");
                TestHarness.Skip("Canvas_ZeroOrSingleLayerConfig",
                    "同上，无法在运行期改动层级配置，因此层数为 0/1 的边界不可构造");
                TestHarness.Skip("Canvas_ExtremeAspectRatio",
                    "Play 模式下无法改变 Game 视图的尺寸与宽高比，只能在当前屏幕比例下验证 Expand");
                TestHarness.Skip("Canvas_MissingCanvasScaler",
                    "各层 Canvas 由 UIRoot 统一创建，必然带 CanvasScaler，无法构造缺失的层");
                TestHarness.Skip("Regression_NoRecordedBugs",
                    "尚无历史缺陷用例；发现缺陷后在此保留具名回归测试");
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
            TestHarness.Summary("Canvas");
        }

        // ---------- Setup / Teardown ----------

        private static void SetupOnce()
        {
            if (_initialized)
                return;
            _initialized = true;

            ResourceGroupFactory.SetFactory(new FakeFactory(PrefabAssets));
            _ = UIRoot.Instance; // 触发 Awake：创建 EventSystem 与各层 Canvas 及管理器
            TestHarness.Require(SingletonUIManager.Instance != null, "UIRoot 初始化后应能取到 SingletonUIManager");
            TestHarness.Require(PopupManager.Instance != null, "UIRoot 初始化后应能取到 PopupManager");

            Register<TestView>(PathView);
            Register<TestPopup>(PathPopup);
        }

        /// <summary>预制体源保持未激活：查存活实例时不必再排除源物体，实例的激活由 ShowInternal 负责。</summary>
        private static void Register<T>(string path) where T : Component
        {
            GameObject source = new GameObject($"Prefab_{typeof(T).Name}");
            source.AddComponent<T>();
            source.SetActive(false);
            PrefabSources.Add(source);
            PrefabAssets[path] = source;
            UIViewConfig.Paths[typeof(T)] = path;
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

        /// <summary>每个用例前的状态复位：关掉全部界面，等上一用例的实例销毁完毕。</summary>
        private static async Task ResetState()
        {
            if (!Application.isPlaying)
                throw new OperationCanceledException("Play 模式已退出，中止测试套件");

            SingletonUIManager.Instance.Clear();
            PopupManager.Instance.Clear();
            await TestHarness.WaitFor(() => CountLiveInstances() == 0, 3000, "等待上一用例的界面实例销毁完毕");
        }

        /// <summary>存活的界面实例数。预制体源未激活，因此不会被计入。</summary>
        private static int CountLiveInstances()
        {
            return Object.FindObjectsOfType<TestView>(false).Length
                 + Object.FindObjectsOfType<TestPopup>(false).Length;
        }

        // ---------- 冒烟 / 契约 ----------

        /// <summary>文档第 4 条：按配置逐层实例化 Canvas，索引即顺序、越往后越在上方。</summary>
        private static Task Canvas_Layers_OneCanvasPerLayerInOrder()
        {
            Canvas[] layers = Layers();
            int expected = UIManagerConfig.ManagerTypes.Length;
            TestHarness.Require(layers.Length == expected,
                $"层数应与 UIManagerConfig 一致：配置 {expected} 层，实际 {layers.Length} 个 Canvas");

            for (int i = 1; i < layers.Length; i++)
            {
                TestHarness.Require(layers[i].sortingOrder > layers[i - 1].sortingOrder,
                    "层级顺序应越往后越在上方（sortingOrder 递增），"
                    + $"实际 {layers[i - 1].name}={layers[i - 1].sortingOrder} → {layers[i].name}={layers[i].sortingOrder}");
            }

            foreach (Canvas canvas in layers)
            {
                TestHarness.Require(canvas.GetComponent<CanvasScaler>() != null,
                    $"每层 Canvas 都应带 CanvasScaler（缩放设置挂在它上面），{canvas.name} 上没有");
            }

            return Task.CompletedTask;
        }

        // ---------- 缩放配置 ----------

        /// <summary>文档第 4 条：Canvas 的缩放设为"UI 元素随屏幕大小缩放"。</summary>
        private static Task CanvasScaler_UiScaleModeIsScaleWithScreenSize()
        {
            foreach (Canvas canvas in Layers())
            {
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                TestHarness.Require(scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize,
                    $"{canvas.name} 的 UI Scale Mode 应为 ScaleWithScreenSize（UI 元素随屏幕大小缩放），实际 {scaler.uiScaleMode}");
            }

            return Task.CompletedTask;
        }

        /// <summary>文档第 4 条：匹配模式为 Expand —— 保证设计稿完整可见、多余空间交给锚点。</summary>
        private static Task CanvasScaler_ScreenMatchModeIsExpand()
        {
            Vector2 reference = UIManagerConfig.ReferenceResolution;
            foreach (Canvas canvas in Layers())
            {
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                TestHarness.Require(scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Expand,
                    $"{canvas.name} 的 Screen Match Mode 应为 Expand，实际 {scaler.screenMatchMode}");
                TestHarness.Require(scaler.referenceResolution == reference,
                    $"{canvas.name} 的参考分辨率应与 UIManagerConfig.ReferenceResolution 同源（{reference}），实际 {scaler.referenceResolution}");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Expand 的实际效果：缩放系数取宽高比中较小者，于是画布在局部坐标系里不小于设计稿、
        /// 且较小比例那一轴恰好等于设计稿；设计稿矩形的四角也落在屏幕之内。
        /// 前两条把 Expand 与 Shrink 分开（Shrink 两轴都不大于设计稿，会裁内容），
        /// 第三条不依赖"画布局部尺寸 = 屏幕像素 ÷ 系数"这个中间机制，直接验证最终结果。
        /// </summary>
        private static async Task CanvasScaler_Expand_KeepsDesignAreaVisible()
        {
            await SettleFrames();

            Vector2 reference = UIManagerConfig.ReferenceResolution;
            float widthRatio = Screen.width / reference.x;
            float heightRatio = Screen.height / reference.y;
            float expectedScale = Mathf.Min(widthRatio, heightRatio); // Expand = Min，源码 CanvasScaler.HandleScaleWithScreenSize
            TestHarness.Require(expectedScale > 0f,
                $"屏幕尺寸异常（Screen={Screen.width}x{Screen.height}，设计稿={reference}），无法验证缩放");

            foreach (Canvas canvas in Layers())
            {
                TestHarness.Require(Mathf.Abs(canvas.scaleFactor - expectedScale) <= 0.001f,
                    $"{canvas.name} 的缩放系数应为 {expectedScale}（Expand 取宽高比较小者：屏幕 {Screen.width}x{Screen.height} ÷ 设计稿 {reference}），实际 {canvas.scaleFactor}");

                Rect rect = ((RectTransform)canvas.transform).rect;
                TestHarness.Require(rect.width >= reference.x - 1f && rect.height >= reference.y - 1f,
                    $"{canvas.name} 的画布局部尺寸 {rect.width}x{rect.height} 不应小于设计稿 {reference}（Expand 保证设计稿完整可见）");

                bool oneAxisMatchesReference =
                    Mathf.Abs(rect.width - reference.x) <= 1f || Mathf.Abs(rect.height - reference.y) <= 1f;
                TestHarness.Require(oneAxisMatchesReference,
                    $"{canvas.name} 的画布局部尺寸应有一轴恰好等于设计稿（Expand 取的是较小比例，那一轴不多不少），实际 {rect.width}x{rect.height}，设计稿 {reference}");

                RequireDesignAreaInsideScreen(canvas, reference);
            }
        }

        /// <summary>
        /// 把设计稿矩形（以画布中心为中心）的四角从画布局部坐标转到屏幕坐标，断言全落在屏幕内。
        /// ScreenSpaceOverlay 下画布的世界坐标就是屏幕像素，所以用 null 相机转换即可。
        /// </summary>
        private static void RequireDesignAreaInsideScreen(Canvas canvas, Vector2 reference)
        {
            RectTransform rect = (RectTransform)canvas.transform;
            float halfWidth = reference.x * 0.5f;
            float halfHeight = reference.y * 0.5f;
            var corners = new[]
            {
                new Vector2(-halfWidth, -halfHeight),
                new Vector2(halfWidth, -halfHeight),
                new Vector2(-halfWidth, halfHeight),
                new Vector2(halfWidth, halfHeight),
            };

            foreach (Vector2 corner in corners)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(corner));
                TestHarness.Require(
                    screenPoint.x >= -0.5f && screenPoint.x <= Screen.width + 0.5f
                    && screenPoint.y >= -0.5f && screenPoint.y <= Screen.height + 0.5f,
                    $"{canvas.name} 的设计稿角点 {corner} 转换成屏幕坐标后落在屏幕外（{screenPoint}，屏幕 {Screen.width}x{Screen.height}），"
                    + "说明 Expand 下设计稿并非完整可见");
            }
        }

        // ---------- 生命周期 ----------

        /// <summary>重复获取不应重复创建：UIRoot 全局唯一，反复访问不产生额外层或额外 EventSystem。</summary>
        private static Task UIRoot_RepeatedAccess_NoExtraLayers()
        {
            int layersBefore = Layers().Length;
            int eventSystemsBefore = Object.FindObjectsOfType<EventSystem>(true).Length;

            for (int i = 0; i < 1000; i++)
            {
                UIRoot root = UIRoot.Instance;
                TestHarness.Require(root != null, $"第 {i} 次访问 UIRoot.Instance 得到空引用");
            }

            TestHarness.Require(Layers().Length == layersBefore,
                $"反复访问 UIRoot.Instance 不应新增层：访问前 {layersBefore} 层，访问后 {Layers().Length} 层");
            TestHarness.Require(Object.FindObjectsOfType<EventSystem>(true).Length == eventSystemsBefore,
                $"反复访问 UIRoot.Instance 不应新增 EventSystem：访问前 {eventSystemsBefore} 个，访问后 {Object.FindObjectsOfType<EventSystem>(true).Length} 个");

            return Task.CompletedTask;
        }

        /// <summary>
        /// 文档第 10 条：场景切换时通知 UIRoot 清理界面。
        /// 清理要清掉界面本身（并走完 Close 生命周期），但不能清掉层 Canvas，清理之后还必须能继续用。
        /// </summary>
        private static async Task UIRoot_CleanupForSceneSwitch_ClearsViewsKeepsLayers()
        {
            await ResetState();
            int layersBefore = Layers().Length;

            var views = SingletonUIManager.Instance;
            views.Open<TestView>();
            UINode node = views.Find(typeof(TestView));
            await TestHarness.WaitFor(() => node != null && node.UI != null, 2000, "界面加载完成");
            TestView view = (TestView)node.UI;

            UIRoot.Instance.CleanupForSceneSwitch();

            TestHarness.Require(views.Find(typeof(TestView)) == null, "清理后界面应从栈中移除");
            TestHarness.Require(view.Closed, "清理应走完界面的 Close 生命周期（OnClose 被调用）");
            TestHarness.Require(Layers().Length == layersBefore,
                $"清理不应销毁层 Canvas：清理前 {layersBefore} 层，清理后 {Layers().Length} 层");

            // 反复清理应安全
            UIRoot.Instance.CleanupForSceneSwitch();
            UIRoot.Instance.CleanupForSceneSwitch();

            // 清理之后层必须仍可用：再开一个界面能正常加载并显示
            views.Open<TestView>();
            UINode reopened = views.Find(typeof(TestView));
            await TestHarness.WaitFor(() => reopened != null && reopened.UI != null, 2000, "清理后重新打开界面");
            views.Show<TestView>();
            TestHarness.Require(reopened.UI.IsShown, "清理之后界面应仍能正常显示");
        }

        /// <summary>文档第 10、12 条：UIRoot 全局唯一、不随场景切换销毁，并负责创建 EventSystem。</summary>
        private static Task UIRoot_DontDestroyOnLoadSingleton_CreatesEventSystem()
        {
            UIRoot root = UIRoot.Instance;
            TestHarness.Require(root != null, "UIRoot.Instance 应可获取");
            TestHarness.Require(ReferenceEquals(root, UIRoot.Instance), "UIRoot 应全局唯一，重复获取应得到同一实例");

            string sceneName = root.gameObject.scene.name;
            TestHarness.Require(sceneName == "DontDestroyOnLoad",
                $"UIRoot 应挂在 DontDestroyOnLoad 场景（当前 \"{sceneName}\"），否则场景切换会连带销毁整个 UI");

            TestHarness.Require(root.GetComponentInChildren<EventSystem>(true) != null,
                "UIRoot 之下应有一个 EventSystem（文档第 12 条由 UIRoot 负责创建）。"
                + "若失败通常说明当前场景自带了一个 EventSystem，UIRoot 检测到就跳过了创建，"
                + "而场景自带的那份会随场景切换被销毁，UI 的输入随之失效");

            return Task.CompletedTask;
        }

        // ---------- 集成 ----------

        /// <summary>
        /// 分层的实际意义：单例界面进低层 Canvas、弹窗进高层 Canvas，且弹窗层排在界面层之上。
        /// 这是"越往后越在上方"唯一的端到端证据——前面的顺序断言只看了 Canvas 自身的排序号。
        /// </summary>
        private static async Task Integration_ViewAndPopup_GoToTheirOwnLayerCanvas()
        {
            await ResetState();

            Canvas[] layers = Layers();
            var views = SingletonUIManager.Instance;
            views.Open<TestView>();
            UINode node = views.Find(typeof(TestView));
            await TestHarness.WaitFor(() => node != null && node.UI != null, 2000, "界面加载完成");

            Canvas viewLayer = node.UI.transform.parent.GetComponent<Canvas>();
            TestHarness.Require(viewLayer != null, "界面应挂到某个层 Canvas 之下");
            TestHarness.Require(Array.IndexOf(layers, viewLayer) >= 0,
                $"界面挂在的 Canvas（{viewLayer.name}）应是 UIRoot 创建的层之一");

            var popups = PopupManager.Instance;
            TestHarness.Require(popups.Push<TestPopup, int>(1), "弹窗 Push 应受理");
            UINode popupNode = popups.Find(typeof(TestPopup));
            await TestHarness.WaitFor(() => popupNode != null && popupNode.UI != null, 2000, "弹窗加载完成");

            Canvas popupLayer = popupNode.UI.transform.parent.GetComponent<Canvas>();
            TestHarness.Require(popupLayer != null, "弹窗应挂到某个层 Canvas 之下");
            TestHarness.Require(Array.IndexOf(layers, popupLayer) >= 0,
                $"弹窗挂在的 Canvas（{popupLayer.name}）应是 UIRoot 创建的层之一");
            TestHarness.Require(popupLayer.sortingOrder > viewLayer.sortingOrder,
                $"弹窗层应排在界面层之上：界面层 {viewLayer.name}={viewLayer.sortingOrder}，弹窗层 {popupLayer.name}={popupLayer.sortingOrder}");
        }

        // ---------- 辅助 ----------

        /// <summary>UIRoot 的层 Canvas 都是它的直接子物体，创建顺序即层级顺序。</summary>
        private static Canvas[] Layers()
        {
            return UIRoot.Instance.GetComponentsInChildren<Canvas>(true);
        }

        /// <summary>
        /// 等若干帧。CanvasScaler 在 Canvas.preWillRenderCanvases 上挂了自己的处理（源码 CanvasScaler.OnEnable），
        /// 所以设置项要到下一次画布渲染才生效——配置写完立刻断言会读到旧值。
        /// </summary>
        private static async Task SettleFrames()
        {
            for (int i = 0; i < 10; i++)
            {
                if (!Application.isPlaying)
                    throw new OperationCanceledException("Play 模式已退出，中止测试套件");
                await Task.Delay(10);
            }
        }
    }
}
