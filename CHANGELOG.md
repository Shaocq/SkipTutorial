# 更新日志

本项目遵循 [语义化版本](https://semver.org/lang/zh-CN/) 与 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/) 格式。

## [0.1.0] - 2026-10-23

首次开源发布。

### 新增

- **新建存档跳过教学关**：捏脸结束后直接进入基地，不进入 `Level_Guide`
- **补发 3000 启动资金**，避免跳过教学后开局没钱
- 序章剧情与捏脸流程完整保留
- 附 `Diagnostics.cs` 诊断开关，可输出存档标记与场景切换日志

### 说明

- 不修改任务进度、物品、数值与存档结构；老存档不受影响
- 依赖前置 HarmonyLib（工坊 `3589088839`），不内嵌 Harmony
