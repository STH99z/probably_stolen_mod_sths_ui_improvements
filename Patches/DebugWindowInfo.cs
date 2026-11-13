using System.Text;
using UnityEngine;

namespace STHs_UI_Improvements
{
    /// <summary>
    /// 开发者模式下的左上角调试：列出当前所有可见 PixelWindow，并在吸附发生时绘制矩形框
    /// （绿色 = 被拖窗口，橙色 = 被吸附到的窗口，仅吸附时显示）。
    /// </summary>
    public class DebugWindowInfo : MonoBehaviour
    {
        private readonly StringBuilder sb = new StringBuilder();
        private static Texture2D whiteTex;

        static Texture2D WhiteTex()
        {
            if (whiteTex == null)
            {
                whiteTex = new Texture2D(1, 1);
                whiteTex.SetPixel(0, 0, Color.white);
                whiteTex.Apply();
            }
            return whiteTex;
        }

        void OnGUI()
        {
            if (!ModSettings.IsDeveloperMode()) return;

            // 1) 吸附比对矩形框
            if (WindowSnapPatch.DebugActive)
            {
                OutlineRect(WindowSnapPatch.DebugMyRect, new Color(0.25f, 0.95f, 0.35f, 0.9f), 2f);
                foreach (Rect r in WindowSnapPatch.DebugOthers)
                {
                    OutlineRect(r, new Color(1f, 0.55f, 0.1f, 0.9f), 2f);
                }
            }

            // 2) 窗口列表文本
            if (WindowsHandler.current == null) return;
            var windows = WindowsHandler.current.visibleWindows;
            if (windows == null) return;

            sb.Clear();
            sb.AppendLine("STH windows (" + windows.Count + "):");
            for (int i = 0; i < windows.Count; i++)
            {
                PixelWindow w = windows[i];
                if (w == null) { sb.AppendLine(i + ": <null>"); continue; }

                string title = string.IsNullOrEmpty(w.titleString) ? "<untitled>" : w.titleString;
                string parentId = "-";
                if (w.parentItems != null && w.parentItems.Count > 0 && w.parentItems[0] != null)
                {
                    parentId = w.parentItems[0].identifier;
                }
                sb.AppendLine(i + ": \"" + title + "\" (" + parentId + ") " + w.widthPixels + "x" + w.heightPixels
                    + " move=" + w.canMove + " " + w.style);
            }

            float h = 18f * (windows.Count + 2) + 10f;
            GUI.Box(new Rect(6, 6, 600, h), GUIContent.none);
            GUI.Label(new Rect(12, 10, 590, h - 8), sb.ToString());
        }

        void FillRect(Rect r, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(r, WhiteTex());
            GUI.color = Color.white;
        }

        void OutlineRect(Rect r, Color c, float t)
        {
            FillRect(new Rect(r.x, r.y, r.width, t), c);          // top
            FillRect(new Rect(r.x, r.yMax - t, r.width, t), c);   // bottom
            FillRect(new Rect(r.x, r.y, t, r.height), c);         // left
            FillRect(new Rect(r.xMax - t, r.y, t, r.height), c);  // right
        }
    }
}
