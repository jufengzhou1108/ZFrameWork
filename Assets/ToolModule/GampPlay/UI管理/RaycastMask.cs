using UnityEngine;
using UnityEngine.UI;

namespace ZFrameWork
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class RaycastMask : Graphic
    {
        protected override void UpdateGeometry()
        {
            // 不生成任何网格：渲染零 overdraw，几何重建零分配
        }
    }
}
