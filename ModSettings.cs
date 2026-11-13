using System;
using UnityEngine;

namespace STHs_UI_Improvements
{
    /// <summary>
    /// 模组配置：按功能分栏，开关放在各栏首个位置。全部收在 ModConfig 面板。
    /// 面板入口：主菜单 → Mods 菜单 → 选中本 mod → 齿轮按钮。
    /// </summary>
    public static class ModSettings
    {
        // —— 窗口吸附 ——
        public static ConfigEntry<bool> EnableSnap;
        public static ConfigEntry<string> SnapMode;
        public static ConfigEntry<string> SnapKey;
        public static ConfigEntry<float> SnapThreshold;

        // —— 打开置顶 ——
        public static ConfigEntry<bool> EnableAlwaysOpenFront;

        // —— 框选 ——
        public static ConfigEntry<bool> EnableBoxSelect;
        public static ConfigEntry<string> BoxSelectKey;

        private static readonly string[] ModifierKeyChoices =
        {
            "LeftCtrl", "LeftShift", "LeftAlt",
            "RightCtrl", "RightShift", "RightAlt"
        };

        private static readonly string[] SnapModeChoices = { "自动", "按键" };

        public static void Load(ModManifest manifest)
        {
            string id = manifest.ID;
            ModConfig config = ModHelper.GetConfig(id);

            // 窗口吸附
            EnableSnap = config.Define("enable_snap", true, "Window Snap",
                () => ModHelper.GetLocalized(id, "opt_enable_snap"),
                tooltip: () => ModHelper.GetLocalized(id, "opt_enable_snap_tip"));

            SnapMode = config.Define("snap_mode", "自动", "Window Snap",
                () => ModHelper.GetLocalized(id, "opt_snap_mode"),
                choices: SnapModeChoices,
                tooltip: () => ModHelper.GetLocalized(id, "opt_snap_mode_tip"));

            SnapKey = config.Define("snap_key", "LeftShift", "Window Snap",
                () => ModHelper.GetLocalized(id, "opt_snap_key"),
                choices: ModifierKeyChoices,
                tooltip: () => ModHelper.GetLocalized(id, "opt_snap_key_tip"));

            SnapThreshold = config.Define("snap_threshold", 16f, "Window Snap",
                () => ModHelper.GetLocalized(id, "opt_snap_threshold"),
                tooltip: () => ModHelper.GetLocalized(id, "opt_snap_threshold_tip"));

            // 打开置顶
            EnableAlwaysOpenFront = config.Define("always_open_window_front", true, "Open Front",
                () => ModHelper.GetLocalized(id, "opt_always_open_window_front"),
                tooltip: () => ModHelper.GetLocalized(id, "opt_always_open_window_front_tip"));

            // 框选
            EnableBoxSelect = config.Define("enable_box_select", true, "Box Select",
                () => ModHelper.GetLocalized(id, "opt_enable_box_select"),
                tooltip: () => ModHelper.GetLocalized(id, "opt_enable_box_select_tip"));

            BoxSelectKey = config.Define("box_select_key", "LeftCtrl", "Box Select",
                () => ModHelper.GetLocalized(id, "opt_box_select_key"),
                choices: ModifierKeyChoices,
                tooltip: () => ModHelper.GetLocalized(id, "opt_box_select_key_tip"));
        }

        public static bool IsBoxSelectKeyHeld()
        {
            return Input.GetKey(ResolveKey(BoxSelectKey.Value));
        }

        /// <summary>窗口吸附是否为「按键」模式。</summary>
        public static bool IsSnapKeyMode()
        {
            return SnapMode.Value == "按键";
        }

        /// <summary>窗口吸附按键是否按下。</summary>
        public static bool IsSnapKeyHeld()
        {
            return Input.GetKey(ResolveKey(SnapKey.Value));
        }

        /// <summary>本 mod 是否处于「开发者模式」（Mods 菜单详情面板里开启）。</summary>
        public static bool IsDeveloperMode()
        {
            if (ModEntry.Instance == null) return false;
            ModTrustRecord record = ModTrust.Get(ModEntry.Instance.Manifest.ID);
            return record != null && record.developerMode;
        }

        static KeyCode ResolveKey(string key)
        {
            switch (key)
            {
                case "LeftShift": return KeyCode.LeftShift;
                case "LeftAlt": return KeyCode.LeftAlt;
                case "RightCtrl": return KeyCode.RightControl;
                case "RightShift": return KeyCode.RightShift;
                case "RightAlt": return KeyCode.RightAlt;
                case "LeftCtrl":
                default: return KeyCode.LeftControl;
            }
        }
    }
}
