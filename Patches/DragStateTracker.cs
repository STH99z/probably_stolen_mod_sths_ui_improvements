using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace STHs_UI_Improvements
{
    /// <summary>
    /// 追踪 Unity EventSystem 是否正在拖拽。TreeNodeRender 实现了 IBeginDragHandler / IDragHandler /
    /// IEndDragHandler，PixelWindow 的背景节点就是一个 TreeNodeRender，其拖拽经由这些回调分发。
    /// 用于区分「拖拽窗口」与「双击打开窗口」（打开时也会调用 FixBounds，但不是拖拽）。
    /// </summary>
    public static class DragStateTracker
    {
        public static bool IsDragging { get; private set; }

        [HarmonyPatch(typeof(TreeNodeRender), "OnBeginDrag")]
        static class BeginPatch
        {
            static void Postfix() => IsDragging = true;
        }

        [HarmonyPatch(typeof(TreeNodeRender), "OnEndDrag")]
        static class EndPatch
        {
            static void Postfix()
            {
                IsDragging = false;
                SnapGuide.Hide();
            }
        }

        [HarmonyPatch(typeof(TreeNodeRender), "OnPointerUp")]
        static class PointerUpPatch
        {
            static void Postfix()
            {
                IsDragging = false;
                SnapGuide.Hide();
            }
        }
    }
}
