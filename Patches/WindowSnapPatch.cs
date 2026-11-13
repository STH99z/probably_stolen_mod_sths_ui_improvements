using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace STHs_UI_Improvements
{
    /// <summary>
    /// 需求 1：PixelWindow 拖拽时自动吸附到屏幕边缘、其他窗口边缘与「顶栏下沿」。
    ///
    /// 关键点：
    /// - PixelWindow.rectTransform（= handler.rectTransform）尺寸退化，真实窗口尺寸在 outsideBorder。
    /// - 每个窗口除整窗外，还额外提供「顶栏」矩形（顶部标题条）。顶栏的左右边、顶边与整窗重合，
    ///   只有「下沿」是一条新线，用于让上方的窗口对齐到它的顶栏下沿、盖住顶栏省空间。
    /// - 顶栏下沿 = 内容区 inner 的世界矩形顶边（inner 紧贴标题栏下方）。
    /// - 无顶栏（无标题/不可移动/无关闭回调）时退回整窗（titleBarBottom == top，成为冗余目标，无害）。
    ///
    /// 只对 canMove=true 的窗口做吸附比对。算法：intervalGap(≤threshold) 门控 + X/Y 独立 nearestSnap + 越界排除。
    /// </summary>
    [HarmonyPatch(typeof(PixelWindow), "FixBounds")]
    public static class WindowSnapPatch
    {
        private static readonly Vector3[] Corners = new Vector3[4];

        // 开发者模式调试用：仅在吸附发生时记录矩形（GUI 屏幕坐标）
        public static bool DebugActive;
        public static Rect DebugMyRect;
        public static readonly List<Rect> DebugOthers = new List<Rect>();

        private struct SnapTarget
        {
            public float position;
            public PixelWindow window; // null = 屏幕边界
        }

        static void Postfix(PixelWindow __instance)
        {
            DebugActive = false;
            DebugOthers.Clear();
            DebugMyRect = default(Rect);

            if (__instance == null) return;
            if (!ModSettings.EnableSnap.Value) return;
            if (!DragStateTracker.IsDragging || !Input.GetMouseButton(0)) return;

            // 「按键」模式：未按住窗口吸附按键时不检测吸附（松开即退回原版逻辑）
            if (ModSettings.IsSnapKeyMode() && !ModSettings.IsSnapKeyHeld())
            {
                SnapGuide.Hide();
                return;
            }

            float threshold = ModSettings.SnapThreshold.Value;
            if (threshold <= 0f) return;

            // 我的窗口：整窗矩形 + 顶栏下沿
            if (!TryGetWindowRect(__instance, out float wL, out float wR, out float wB, out float wT, out float wTb)) return;

            RectTransform bounds = GetBoundsRectTransform(__instance);
            if (bounds == null) return;
            bounds.GetWorldCorners(Corners);
            float sL = Corners[0].x, sR = Corners[2].x, sB = Corners[0].y, sT = Corners[1].y;

            float wW = wR - wL, wH = wT - wB;
            float maxX = Mathf.Max(sL, sR - wW);
            float maxY = Mathf.Max(sB, sT - wH);

            float x = wL, y = wB;
            float right = x + wW, bottom = y + wH;

            List<SnapTarget> xTargets = new List<SnapTarget>();
            List<SnapTarget> yTargets = new List<SnapTarget>();

            List<PixelWindow> windows = WindowsHandler.current != null ? WindowsHandler.current.visibleWindows : null;
            if (windows != null)
            {
                foreach (PixelWindow other in windows)
                {
                    if (other == null || other == __instance || !other.IsVisible()) continue;
                    if (!other.canMove) continue;
                    if (!TryGetWindowRect(other, out float oL, out float oR, out float oB, out float oT, out float oTb)) continue;

                    // 垂直方向足够近 → 对方左右边进入 X 候选
                    if (IntervalGap(y, bottom, oB, oT) <= threshold)
                    {
                        xTargets.Add(new SnapTarget { position = oL, window = other });
                        xTargets.Add(new SnapTarget { position = oR, window = other });
                    }

                    // 水平方向足够近 → 对方上下边 + 顶栏下沿进入 Y 候选
                    if (IntervalGap(x, right, oL, oR) <= threshold)
                    {
                        yTargets.Add(new SnapTarget { position = oB, window = other });
                        yTargets.Add(new SnapTarget { position = oT, window = other });
                        yTargets.Add(new SnapTarget { position = oTb, window = other });
                    }
                }
            }

            // 边界也作为候选（window = null）
            xTargets.Add(new SnapTarget { position = sL });
            xTargets.Add(new SnapTarget { position = sR });
            yTargets.Add(new SnapTarget { position = sB });
            yTargets.Add(new SnapTarget { position = sT });

            float snapX = NearestSnap(new[] { x, right }, xTargets, threshold, sL, maxX, x, out float guideX, out PixelWindow ownerX);
            float snapY = NearestSnap(new[] { y, bottom, wTb }, yTargets, threshold, sB, maxY, y, out float guideY, out PixelWindow ownerY);

            if (snapX != 0f) { SnapGuide.ShowX(guideX); } else { SnapGuide.HideX(); }
            if (snapY != 0f) { SnapGuide.ShowY(guideY); } else { SnapGuide.HideY(); }

            if (snapX != 0f || snapY != 0f)
            {
                Vector3 pos = __instance.rectTransform.position;
                pos.x += snapX;
                pos.y += snapY;
                __instance.rectTransform.position = pos;

                DebugActive = true;
                DebugMyRect = ToScreenRect(wL, wR, wB, wT);
                AddDebugOther(ownerX);
                if (ownerY != null && ownerY != ownerX) AddDebugOther(ownerY);
            }
        }

        static void AddDebugOther(PixelWindow w)
        {
            if (w == null) return;
            if (TryGetWindowRect(w, out float l, out float r, out float b, out float t, out _))
            {
                DebugOthers.Add(ToScreenRect(l, r, b, t));
            }
        }

        // 取窗口的真实世界矩形（outsideBorder）+ 顶栏下沿（inner 顶边）
        static bool TryGetWindowRect(PixelWindow window, out float left, out float right, out float bottom, out float top, out float titleBarBottom)
        {
            left = right = bottom = top = titleBarBottom = 0f;
            if (window == null) return false;

            TreeNodeRender border = ModReflect.Get<TreeNodeRender>(window, "outsideBorder");
            if (border == null || border.rectTransform == null) return false;

            border.rectTransform.GetWorldCorners(Corners);
            left = Corners[0].x;
            right = Corners[2].x;
            bottom = Corners[0].y;
            top = Corners[1].y;

            // 顶栏下沿：
            // - Machinery 风格：用标题头 machineryHeader 的下沿（始终 20px，与内容类型无关，
            //   避免裸内容窗口多 8px 内边距导致顶栏被量偏大）。
            // - Classic 风格：用内容区 inner 的上沿（无标题/不可移动/无关闭回调时退回整窗顶边）。
            if (window.style == WindowStyle.Machinery)
            {
                TreeNodeRender header = ModReflect.Get<TreeNodeRender>(window, "machineryHeader");
                if (header != null && header.rectTransform != null)
                {
                    header.rectTransform.GetWorldCorners(Corners);
                    titleBarBottom = Corners[0].y; // 标题头下沿
                }
                else
                {
                    titleBarBottom = top;
                }
            }
            else
            {
                bool hasTitleBar = window.canMove || window.hasTitle || window.closeCallback != null;
                if (hasTitleBar && window.inner != null && window.inner.rectTransform != null)
                {
                    window.inner.rectTransform.GetWorldCorners(Corners);
                    titleBarBottom = Corners[1].y; // inner 上沿
                }
                else
                {
                    titleBarBottom = top;
                }
            }

            return true;
        }

        // 世界坐标（y 向上）→ GUI 屏幕坐标（y 向下）
        static Rect ToScreenRect(float left, float right, float bottom, float top)
        {
            float guiTop = Screen.height - top;
            return new Rect(left, guiTop, right - left, top - bottom);
        }

        // 两个区间 [aMin,aMax] 与 [bMin,bMax] 之间的间隙（重叠时为 0）
        static float IntervalGap(float aMin, float aMax, float bMin, float bMax)
        {
            return Mathf.Max(0f, aMin - bMax, bMin - aMax);
        }

        // 对某一轴：在候选 target 中，为我的每条边找最近、且吸附后仍在 [min,max] 内的目标
        static float NearestSnap(float[] edges, List<SnapTarget> targets, float threshold, float min, float max, float position, out float guide, out PixelWindow owner)
        {
            float bestOffset = 0f;
            float bestDist = float.PositiveInfinity;
            guide = float.NaN;
            owner = null;
            foreach (SnapTarget t in targets)
            {
                foreach (float edge in edges)
                {
                    float offset = t.position - edge;
                    float dist = Mathf.Abs(offset);
                    float nextPos = position + offset;
                    if (nextPos >= min && nextPos <= max && dist <= threshold && dist < bestDist)
                    {
                        bestOffset = offset;
                        guide = t.position;
                        owner = t.window;
                        bestDist = dist;
                    }
                }
            }
            return bestOffset;
        }

        static RectTransform GetBoundsRectTransform(PixelWindow window)
        {
            Canvas canvas = window.rectTransform.GetComponentInParent<Canvas>(true);
            if (canvas == null)
            {
                canvas = RenderHandler.current != null ? RenderHandler.current.windowUICanvas : null;
            }
            else
            {
                canvas = canvas.rootCanvas;
            }
            return canvas != null ? canvas.GetComponent<RectTransform>() : null;
        }
    }
}
