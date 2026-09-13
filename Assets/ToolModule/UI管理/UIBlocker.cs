using UnityEngine.UI;

namespace ZFrameWork
{
    /// <summary>
    /// 零 overdraw、零网格开销的弹窗拦截器。
    /// 射线检测基于 RectTransform 矩形而非网格顶点，因此不生成网格也能拦截点击；
    /// 重写 UpdateGeometry 为空以完全跳过网格生成流水线（VertexHelper / FillMesh 路径），
    /// 不产生任何重建分配。由弹窗管理器把兄弟索引压在栈顶弹窗之下（n-1），实现模态输入阻断。
    /// </summary>
    public class UIBlocker : Graphic
    {
        protected override void UpdateGeometry()
        {
            // 不生成任何网格：渲染零 overdraw，几何重建零分配
        }
    }
}
