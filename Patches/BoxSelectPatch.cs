using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace STHs_UI_Improvements
{
    /// <summary>
    /// 需求 3：在任意格子（无论是否有道具）按住框选键（默认 Ctrl）即可触发框选。
    ///
    /// 原逻辑：ItemMultiSelectHandler.OnEventPress 里只有
    ///     RenderHandler.RaycastElement&lt;GameItemElement&gt;(mousePosition) == null
    /// （即鼠标下无物品、空格子）时才进入 PendingBand（框选）。
    ///
    /// 通过 Transpiler 在该 RaycastElement 调用之后插入 ShouldSkipBand，把
    /// “有物品 → return（不框选）”改为“有物品且未按框选键 → return”。
    /// 即：空格子照常框选；有物品时按住框选键也进入框选。
    /// </summary>
    [HarmonyPatch(typeof(ItemMultiSelectHandler), "OnEventPress")]
    public static class BoxSelectPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> list = instructions.ToList();
            MethodInfo shouldSkip = AccessTools.Method(typeof(BoxSelectPatch), nameof(ShouldSkipBand));

            for (int i = 0; i < list.Count - 1; i++)
            {
                if (list[i].opcode == OpCodes.Call
                    && list[i].operand is MethodInfo mi
                    && mi.IsGenericMethod
                    && mi.Name == "RaycastElement"
                    && mi.DeclaringType == typeof(RenderHandler)
                    && mi.GetGenericArguments().Length == 1
                    && mi.GetGenericArguments()[0] == typeof(GameItemElement)
                    && (list[i + 1].opcode == OpCodes.Brfalse || list[i + 1].opcode == OpCodes.Brfalse_S))
                {
                    list.Insert(i + 1, new CodeInstruction(OpCodes.Call, shouldSkip));
                    break;
                }
            }

            return list;
        }

        static bool ShouldSkipBand(GameItemElement item)
        {
            if (!ModSettings.EnableBoxSelect.Value)
                return item != null;                  // 功能关闭 → 原行为（仅空格子框选）
            if (item == null) return false;           // 空格子 → 不跳过（正常框选）
            return !ModSettings.IsBoxSelectKeyHeld(); // 有物品 → 仅按住框选键时不跳过
        }
    }
}
