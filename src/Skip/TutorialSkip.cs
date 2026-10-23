using System;
using Duckov.Economy;
using Duckov.Scenes;
using Duckov.Utilities;
using Eflatun.SceneReference;
using HarmonyLib;
using Saves;
using UnityEngine;

namespace SkipTutorial
{
    /// <summary>
    /// 跳过新手引导关卡。
    ///
    /// <para><b>范围：</b>只跳 <c>Level_Guide</c>（新手教学关），
    /// <b>保留序章与捏脸</b>——新档仍然先走 <c>Prologue</c>，捏完脸、剧情结束后
    /// 本来要进 <c>Level_Guide</c>，这里改成直接进基地。</para>
    ///
    /// <para><b>做法：</b>所有场景加载最终都会走
    /// <c>SceneLoader.LoadScene(SceneReference, ...)</c>，在这里把
    /// 「Level_Guide 的主场景」替换成「基地场景」，后续流程照原版继续跑，
    /// 不会打断异步加载的等待链。</para>
    ///
    /// <para><b>同时补写：</b>引导关卡的「到过」标记与三段引导过场的「已播」标记，
    /// 让游戏认为引导已经完成，避免后续逻辑（地图、教练 NPC、任务）等不到状态。</para>
    ///
    /// <para><b>启动资金：</b>到达基地后发放 3000 元（记在 <c>EconomyManager</c> 现金账户），
    /// 用独立的存档键保证每个存档只发一次。</para>
    /// </summary>
    internal static class TutorialSkip
    {
        /// <summary>新手启动资金。</summary>
        internal const long RewardMoney = 3000;

        /// <summary>启动资金是否已发放的存档键（每个存档独立）。</summary>
        private const string RewardKey = "SkipTutorial_RewardGranted";

        /// <summary>引导关卡的主场景名前缀。</summary>
        private const string GuideScenePrefix = "Level_Guide";

        /// <summary>引导关卡用作「是否已跳过」判断的子场景。</summary>
        private const string GuideVisitProbe = "Level_Guide_Main";

        /// <summary>引导关卡的全部子场景（补写「到过」标记用）。</summary>
        private static readonly string[] GuideSubScenes =
        {
            "Level_Guide_Main",
            "Level_Guide_1",
            "Level_Guide_2",
        };

        /// <summary>引导关里的过场（补写「已播」标记用）。</summary>
        private static readonly string[] GuideCutScenes =
        {
            "Guide_Intro",
            "Guide_Intro2",
            "Guide_IntroDodge",
        };

        /// <summary>本次会话是否已经触发过跳过（用来在到基地后发钱）。</summary>
        private static bool _skipTriggered;

        private static bool _installed;

        internal static void Install()
        {
            if (_installed)
                return;

            _installed = true;

            try
            {
                LevelManager.OnLevelInitialized += OnLevelInitialized;
            }
            catch (Exception e)
            {
                Diag.Report("订阅关卡初始化事件失败", e);
            }
        }

        internal static void Uninstall()
        {
            if (!_installed)
                return;

            _installed = false;

            try
            {
                LevelManager.OnLevelInitialized -= OnLevelInitialized;
            }
            catch (Exception e)
            {
                Diag.Report("取消订阅关卡初始化事件失败", e);
            }
        }

        /// <summary>
        /// 如果这次要加载的是新手引导关卡，就换成基地场景。
        /// 只在「本存档还没到过引导关」时生效，避免影响已经打过引导的存档。
        /// </summary>
        internal static void RedirectGuideToBase(ref SceneReference sceneReference)
        {
            try
            {
                if (sceneReference == null)
                    return;

                string sceneName;
                try
                {
                    sceneName = sceneReference.Name;
                }
                catch
                {
                    return;
                }

                if (string.IsNullOrEmpty(sceneName) ||
                    !sceneName.StartsWith(GuideScenePrefix, StringComparison.Ordinal))
                {
                    return;
                }

                if (MultiSceneCore.GetVisited(GuideVisitProbe))
                {
                    // 这个存档已经走过引导，保持原版行为
                    return;
                }

                SceneReference baseScene = GameplayDataSettings.SceneManagement?.BaseScene;
                if (baseScene == null)
                {
                    Diag.Warn("跳过引导失败：没有配置基地场景（SceneManagement/BaseScene），保持原版流程");
                    return;
                }

                MarkGuideCompleted();

                _skipTriggered = true;
                sceneReference = baseScene;

                Diag.Log($"已跳过新手引导：{sceneName} → 基地");
            }
            catch (Exception e)
            {
                Diag.Report("跳过新手引导失败", e);
            }
        }

        /// <summary>补写引导关「到过」与过场「已播」标记。</summary>
        private static void MarkGuideCompleted()
        {
            for (int i = 0; i < GuideSubScenes.Length; i++)
            {
                try
                {
                    MultiSceneCore.SetVisited(GuideSubScenes[i]);
                }
                catch (Exception e)
                {
                    Diag.Report($"补写场景标记失败：{GuideSubScenes[i]}", e);
                }
            }

            for (int i = 0; i < GuideCutScenes.Length; i++)
            {
                try
                {
                    SavesSystem.Save("CutScene_" + GuideCutScenes[i], value: true);
                }
                catch (Exception e)
                {
                    Diag.Report($"补写过度标记失败：{GuideCutScenes[i]}", e);
                }
            }
        }

        private static void OnLevelInitialized()
        {
            try
            {
                if (!_skipTriggered)
                    return;

                LevelManager manager = LevelManager.Instance;
                if (manager == null || !manager.IsBaseLevel)
                    return;

                if (SavesSystem.Load<bool>(RewardKey))
                {
                    _skipTriggered = false;
                    Diag.Log("新手启动资金已发放过，跳过");
                    return;
                }

                if (!EconomyManager.Add(RewardMoney))
                {
                    // 保留 _skipTriggered，等下一次关卡初始化再试
                    Diag.Warn("发放新手启动资金失败：EconomyManager 尚未就绪，稍后重试");
                    return;
                }

                SavesSystem.Save(RewardKey, value: true);
                _skipTriggered = false;
                Diag.Log($"已发放新手启动资金 {RewardMoney}");
            }
            catch (Exception e)
            {
                Diag.Report("发放新手启动资金失败", e);
            }
        }
    }

    /// <summary>
    /// 场景加载拦截：把「新手引导关卡」替换成「基地」。
    ///
    /// 原版所有场景加载（无论是按 SceneInfo ID 还是直接给 SceneReference）
    /// 最终都会走到这个重载，所以在这里替换参数最稳妥，异步等待链保持完整。
    /// </summary>
    [HarmonyPatch(typeof(SceneLoader), "LoadScene", new[]
    {
        typeof(SceneReference),   // sceneReference
        typeof(SceneReference),   // overrideCurtainScene
        typeof(bool),             // clickToConinue
        typeof(bool),             // notifyEvacuation
        typeof(bool),             // doCircleFade
        typeof(bool),             // useLocation
        typeof(MultiSceneLocation), // location
        typeof(bool),             // saveToFile
        typeof(bool),             // hideTips
    })]
    internal static class SkipGuideScenePatch
    {
        [HarmonyPrefix]
        private static void Prefix(ref SceneReference sceneReference)
        {
            TutorialSkip.RedirectGuideToBase(ref sceneReference);
        }
    }
}
