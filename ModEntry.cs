using System;
using UnityEngine;
using HarmonyLib;

namespace STHs_UI_Improvements
{
    /// <summary>
    /// 模组入口。加载器找到实现 IMod 的类，创建它，应用本程序集的 [HarmonyPatch] 类，
    /// 然后依次调用 Init、OnEnable。都发生在主菜单阶段，此时存档未载入，不要读写游戏状态。
    /// </summary>
    public class ModEntry : IMod
    {
        public static ModEntry Instance { get; private set; }
        public ModManifest Manifest { get; private set; }
        public ModLog Log { get; private set; }

        private GameObject debugOverlay;

        public void Init(ModManifest manifest)
        {
            Instance = this;
            Manifest = manifest;
            Log = new ModLog(manifest);

            ModSettings.Load(manifest);

            Log.Log($"{manifest.Name} v{manifest.ModVersion} loaded.");
            Log.Log($"Game version (Application.version): {Application.version}");
        }

        public void OnEnable()
        {
            debugOverlay = new GameObject("STH_DebugWindowInfo");
            debugOverlay.AddComponent<DebugWindowInfo>();
            UnityEngine.Object.DontDestroyOnLoad(debugOverlay);
            Log.Log("Enabled.");
        }

        public void OnDisable()
        {
            if (debugOverlay != null)
            {
                UnityEngine.Object.Destroy(debugOverlay);
                debugOverlay = null;
            }
            Log.Log("Disabled.");
        }
    }
}
