using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace SkipTutorial
{
    /// <summary>
    /// 流程诊断：把「新游戏 → 序章 → 新手引导 → 基地」实际的场景切换、
    /// 关卡初始化与过场播放全部写进 <c>SkipTutorial.log</c>。
    ///
    /// 只读日志，不改变任何行为。等确认清楚引导的触发点后，
    /// 真正的跳过逻辑再按日志结论接入（见 README 的「待确认」一节）。
    /// </summary>
    internal static class Diagnostics
    {
        private static bool _installed;

        internal static void Install()
        {
            if (_installed)
                return;

            _installed = true;

            try
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                SceneManager.sceneUnloaded += OnSceneUnloaded;
                LevelManager.OnLevelInitialized += OnLevelInitialized;
                Diag.Log("流程诊断已开启");
            }
            catch (Exception e)
            {
                Diag.Report("开启流程诊断失败", e);
            }
        }

        internal static void Uninstall()
        {
            if (!_installed)
                return;

            _installed = false;

            try
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
                LevelManager.OnLevelInitialized -= OnLevelInitialized;
            }
            catch (Exception e)
            {
                Diag.Report("关闭流程诊断失败", e);
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                Diag.Log($"场景已加载：{scene.name}（{mode}，buildIndex={scene.buildIndex}）");
            }
            catch (Exception e)
            {
                Diag.Report("记录场景加载失败", e);
            }
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            try
            {
                Diag.Log($"场景已卸载：{scene.name}");
            }
            catch (Exception e)
            {
                Diag.Report("记录场景卸载失败", e);
            }
        }

        private static void OnLevelInitialized()
        {
            try
            {
                LevelManager manager = LevelManager.Instance;
                bool raid = manager != null && manager.IsRaidMap;
                bool based = manager != null && manager.IsBaseLevel;

                Diag.Log($"关卡初始化完成：IsRaidMap={raid}，IsBaseLevel={based}");
            }
            catch (Exception e)
            {
                Diag.Report("记录关卡初始化失败", e);
            }
        }
    }

    /// <summary>记录过场播放（CutScene.MarkPlayed），用于确认哪些剧情被跳过。</summary>
    [HarmonyPatch(typeof(CutScene), nameof(CutScene.MarkPlayed))]
    internal static class CutSceneLogPatch
    {
        private static FieldInfo _idField;

        [HarmonyPostfix]
        private static void Postfix(CutScene __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                if (_idField == null)
                    _idField = AccessTools.Field(typeof(CutScene), "id");

                string id = _idField != null ? _idField.GetValue(__instance) as string : null;
                Diag.Log($"过场已播放：{id ?? __instance.gameObject.name}");
            }
            catch (Exception e)
            {
                Diag.Report("记录过场播放失败", e);
            }
        }
    }
}
