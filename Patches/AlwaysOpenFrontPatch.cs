using HarmonyLib;
using UnityEngine;

namespace STHs_UI_Improvements
{
    /// <summary>
    /// 需求 2：交互（单击/双击）物品打开 UI 时，若窗口已打开则置顶而不是关闭。
    ///
    /// 原逻辑（ItemMouseDoubleClickHandler.ClickAction）在 contentWindow.IsVisible() 时
    /// 调用 CloseContentWindow()（即“已开→关闭”）。这里改为：已开→ToFront()（置顶）。
    /// 未打开时原逻辑本就会走 OpenContentAction（打开并置顶），无需改动。
    /// </summary>
    [HarmonyPatch(typeof(ItemMouseDoubleClickHandler), "ClickAction")]
    public static class AlwaysOpenFrontPatch
    {
        static bool Prefix(GameItem newItem, Vector2 mousePosition, bool interact, bool activate)
        {
            if (!ModSettings.EnableAlwaysOpenFront.Value) return true;
            if (!interact || newItem == null || newItem.contentWindow == null) return true;
            if (!newItem.contentWindow.IsVisible()) return true; // 未打开 → 走原逻辑打开

            // 已打开 → 置顶，而不是关闭
            newItem.contentWindow.ToFront();
            AudioManager.Instance.TryPlayOne(newItem.soundContainerOpen);

            // 复现原方法在窗口分支之后的 activate 逻辑（对带 contentWindow 的物品通常不触发，
            // 但为保持行为一致仍按原样执行）。
            if (activate && newItem.MayActivateSlotItem() && newItem.CanActivateSlotItem())
            {
                newItem.ActivateSlotItem();
            }

            return false; // 跳过原方法（避免其中的 CloseContentWindow）
        }
    }
}
