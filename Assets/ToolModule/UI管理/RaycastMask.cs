using UnityEngine;
using UnityEngine.UI;

namespace ZFrameWork
{
    /// <summary>
    /// 零 overdraw、零网格开销的射线遮罩。
    /// 射线检测基于 RectTransform 矩形而非网格顶点，因此不生成网格也能拦住点击；
    /// 重写 UpdateGeometry 为空以完全跳过网格生成流水线（VertexHelper / FillMesh 路径），
    /// 不产生任何重建分配。由弹窗管理器把兄弟索引压在栈顶弹窗之下（n-1），实现模态输入阻断。
    ///
    /// CanvasRenderer 必须显式声明：Graphic 自身只要求 RectTransform，是 Text/Image/RawImage 各自
    /// 声明了 CanvasRenderer。缺了它，Graphic.canvasRenderer 返回空引用，而 GraphicRaycaster 在
    /// 判定候选时会读 canvasRenderer.cull 与 depth（depth 即 canvasRenderer.absoluteDepth），
    /// 一读就抛 MissingComponentException，射线判定直接中断。
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class RaycastMask : Graphic
    {
        protected override void UpdateGeometry()
        {
            // 不生成任何网格：渲染零 overdraw，几何重建零分配
        }
    }
}
