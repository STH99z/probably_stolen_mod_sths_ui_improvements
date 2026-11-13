using UnityEngine;
using UnityEngine.UI;

namespace STHs_UI_Improvements
{
    /// <summary>
    /// 吸附辅助线：窗口吸附到某条边时，在该边位置画一条贯穿画布的细线。
    /// 竖线 = 左右吸附，横线 = 上下吸附。拖拽结束或不再吸附时隐藏。
    /// </summary>
    public static class SnapGuide
    {
        private static Image vertical;
        private static Image horizontal;
        private static RectTransform verticalRect;
        private static RectTransform horizontalRect;
        private static RectTransform canvasRect;

        static void EnsureCreated()
        {
            Canvas canvas = RenderHandler.current != null ? RenderHandler.current.windowUICanvas : null;
            if (canvas == null) return;
            canvasRect = canvas.GetComponent<RectTransform>();

            if (vertical == null || vertical.gameObject == null)
            {
                GameObject go = new GameObject("STH_SnapGuide_V", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                vertical = go.GetComponent<Image>();
                vertical.color = new Color(0.271f, 0.569f, 0.545f, 0.75f);
                vertical.raycastTarget = false;
                verticalRect = go.GetComponent<RectTransform>();
                verticalRect.SetParent(canvas.transform, false);
                verticalRect.anchorMin = new Vector2(0.5f, 0.5f);
                verticalRect.anchorMax = new Vector2(0.5f, 0.5f);
                verticalRect.pivot = new Vector2(0.5f, 0.5f);
                vertical.enabled = false;
            }

            if (horizontal == null || horizontal.gameObject == null)
            {
                GameObject go = new GameObject("STH_SnapGuide_H", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                horizontal = go.GetComponent<Image>();
                horizontal.color = new Color(0.271f, 0.569f, 0.545f, 0.75f);
                horizontal.raycastTarget = false;
                horizontalRect = go.GetComponent<RectTransform>();
                horizontalRect.SetParent(canvas.transform, false);
                horizontalRect.anchorMin = new Vector2(0.5f, 0.5f);
                horizontalRect.anchorMax = new Vector2(0.5f, 0.5f);
                horizontalRect.pivot = new Vector2(0.5f, 0.5f);
                horizontal.enabled = false;
            }
        }

        public static void ShowX(float worldX)
        {
            EnsureCreated();
            if (verticalRect == null || canvasRect == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, new Vector2(worldX, Screen.height * 0.5f), null, out Vector2 local);
            verticalRect.anchoredPosition = new Vector2(local.x, 0f);
            verticalRect.sizeDelta = new Vector2(2f, canvasRect.rect.height);
            verticalRect.SetAsLastSibling();
            vertical.enabled = true;
        }

        public static void ShowY(float worldY)
        {
            EnsureCreated();
            if (horizontalRect == null || canvasRect == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, new Vector2(Screen.width * 0.5f, worldY), null, out Vector2 local);
            horizontalRect.anchoredPosition = new Vector2(0f, local.y);
            horizontalRect.sizeDelta = new Vector2(canvasRect.rect.width, 2f);
            horizontalRect.SetAsLastSibling();
            horizontal.enabled = true;
        }

        public static void HideX()
        {
            if (vertical != null) vertical.enabled = false;
        }

        public static void HideY()
        {
            if (horizontal != null) horizontal.enabled = false;
        }

        public static void Hide()
        {
            HideX();
            HideY();
        }
    }
}
