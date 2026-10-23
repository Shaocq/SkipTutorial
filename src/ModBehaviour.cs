using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SkipTutorial
{
    /// <summary>
    /// 「跳过新手引导」mod 入口。
    ///
    /// 当前为前期骨架：负责日志、Harmony 前置检测与补丁安装流程。
    /// 具体的跳过逻辑在 <c>src/Skip/</c> 下实现（调研结论见 README.md）。
    ///
    /// 前置：HarmonyLib（工坊 3589088839），只做编译期引用，不内嵌 0Harmony。
    /// </summary>
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private const string HarmonyId = "com.skiptutorial";
        private const float RetryInterval = 2f;

        private static ModBehaviour _instance;
        private static bool _patchAttempted;

        private Harmony _harmony;
        private Coroutine _patchCoroutine;

        /// <summary>当前生效的 mod 实例。</summary>
        internal static ModBehaviour Instance => _instance;

        protected override void OnAfterSetup()
        {
            try
            {
                _instance = this;

                Diag.Log($"已加载：版本 {info.version}，目录 {info.path}");

                Diagnostics.Install();
                TutorialSkip.Install();

                _patchCoroutine = StartCoroutine(InstallHarmonyWhenReady());
            }
            catch (Exception e)
            {
                Diag.Report("初始化失败", e);
            }
        }

        protected override void OnBeforeDeactivate()
        {
            try
            {
                Diagnostics.Uninstall();
                TutorialSkip.Uninstall();

                if (_patchCoroutine != null)
                {
                    StopCoroutine(_patchCoroutine);
                    _patchCoroutine = null;
                }

                if (_harmony != null)
                {
                    _harmony.UnpatchAll(HarmonyId);
                    _harmony = null;
                }

                _patchAttempted = false;
                Diag.Log("已停用，补丁已清理");
            }
            catch (Exception e)
            {
                Diag.Report("停用清理失败", e);
            }
        }

        /// <summary>
        /// 等待前置 HarmonyLib 把 0Harmony 载入后再安装补丁。
        /// 单独拆成方法：0Harmony 未载入时不能让包含 Harmony 类型的方法被 JIT 解析。
        /// </summary>
        private IEnumerator InstallHarmonyWhenReady()
        {
            while (true)
            {
                try
                {
                    if (HarmonyLoad.Load0Harmony() != null)
                    {
                        InstallPatches();
                        yield break;
                    }

                    if (!_patchAttempted)
                    {
                        _patchAttempted = true;
                        Diag.Warn("未找到前置 HarmonyLib（工坊 3589088839），每 2 秒重试一次");
                    }
                }
                catch (Exception e)
                {
                    Diag.Report("安装 Harmony 补丁失败，稍后重试", e);
                }

                yield return new WaitForSecondsRealtime(RetryInterval);
            }
        }

        private void InstallPatches()
        {
            try
            {
                _harmony = new Harmony(HarmonyId);
                _harmony.PatchAll(Assembly.GetExecutingAssembly());

                Diag.Log($"Harmony 补丁已就绪（0Harmony {HarmonyLoad.HarmonyVersion}）");
            }
            catch (Exception e)
            {
                Diag.Report("PatchAll 失败", e);
            }
        }
    }
}
