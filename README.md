# 跳过新手引导（SkipTutorial）

[![游戏](https://img.shields.io/badge/游戏-Escape%20from%20Duckov-blue)](https://store.steampowered.com/app/3167020/)
[![协议](https://img.shields.io/badge/协议-MIT-yellow)](LICENSE)
[![版本](https://img.shields.io/badge/版本-0.1.0-brightgreen)](CHANGELOG.md)

> 新建存档时跳过新手教程引导关卡，捏脸结束后直接进入基地，并补发 3000 启动资金。

| 项目 | 信息 |
| --- | --- |
| 游戏 | 逃离鸭科夫（Escape from Duckov，AppID `3167020`） |
| 框架 | `Duckov.Modding.ModBehaviour` |
| 前置 | HarmonyLib（工坊 `3589088839`），必须置于本 mod **之前**加载 |
| 创意工坊 | — |
| 当前版本 | 0.1.0 |
| 开源协议 | [MIT](LICENSE) |

## 安装

订阅前置 HarmonyLib（工坊 `3589088839`）并置于本 mod **之前**加载，然后把 `SkipTutorial` 文件夹放入 `Duckov_Data\Mods\`。

> **`lib/0Harmony.dll` 不在仓库内。** 需要从源码编译时，请把创意工坊前置 HarmonyLib 目录下的 `0Harmony.dll` 复制到 `lib/` 下。


> 状态：**已实现基础版本**（2026-10-22）。
> 新档保留序章与捏脸，跳过 `Level_Guide` 新手教学关，直接进基地并补发 3000 启动资金。

## 目录结构

| 路径 | 说明 |
| --- | --- |
| `SkipTutorial.csproj` | 工程文件（netstandard2.1，程序集 `SkipTutorial`） |
| `info.ini` | mod 元数据；`name = SkipTutorial` 是内部标识兼 DLL 名，**发布后不要改** |
| `lib/0Harmony.dll` | 前置 HarmonyLib 的编译期引用，**不随 mod 分发** |
| `src/ModBehaviour.cs` | mod 入口：日志、Harmony 前置重试、诊断开关 |
| `src/Diagnostics.cs` | 流程诊断：记录场景加载/卸载、关卡初始化、过场播放 |
| `src/HarmonyLoad.cs` | 查找前置已载入的 0Harmony |
| `src/Diag.cs` | 统一日志出口，日志失败不抛给游戏 |

构建产物：`bin/Release/Mod/SkipTutorial/`（DLL + info.ini）

```powershell
$dn = "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe"
& $dn build 跳过新手引导\SkipTutorial.csproj -c Release
```

## 已查明的机制

### 新游戏入口

`Duckov.UI.MainMenu.ContinueButton.OnButtonClicked()`：

```csharp
GameManager.newBoot = true;

if (MultiSceneCore.GetVisited("Base"))
{
    LoadGame();            // 进过基地 → 当作「继续游戏」
    return;
}

SavesSystem.Save("CreatedWithVersion", GameMetaData.Instance.Version);
SceneLoader.Instance.LoadScene(GameplayDataSettings.SceneManagement.PrologueScene, ...);  // 新档 → 序章
```

也就是说：**新档一律先走 `PrologueScene`**，而不是直接进基地。

### 相关场景（globalgamemanagers 构建顺序）

| 场景 | 说明 |
| --- | --- |
| `Startup/Startup`、`MainMenu/MainMenu` | 启动、主菜单 |
| `Startup/DummyPrologueCutscene` | 占位序章过场 |
| `Prologue/Prologue_Main`、`Prologue/Prologue_1` | 序章（`DreamCustomFace` 等过场应在这里，**可能包含捏脸**） |
| `Level_Guide/Level_Guide_Main`、`_1`、`_2` | 新手引导关卡 |
| `Base`、`Base_Scene`、`Base_SceneV2` | 基地 |

### 存档标记（都是 SavesSystem 顶层 bool）

| 键 | 写入位置 | 含义 |
| --- | --- | --- |
| `MultiSceneCore_Visited_<sceneID>` | `MultiSceneCore.Visited()` 里 `Save` | 该子场景是否到过 |
| `CutScene_<id>` | `CutScene.MarkPlayed()` | 该过场是否播过 |
| `CreatedWithVersion` | 新档点击开始时 | 建档版本 |
| `NewGameReported` | `LevelManager` 首次进入关卡 | 只用于上报，不是引导开关 |

用户现有存档里实测存在：

- `MultiSceneCore_Visited_`：`Prologue_Main`、`Prologue_1`、`Level_Guide_Main`、`Level_Guide_1`、`Level_Guide_2`、`Base` 等
- `CutScene_`：`Guide_Intro`、`Guide_Intro2`、`Guide_IntroDodge`，以及 `Base_Intro`、`DreamCustomFace`、`DreamGoOut`、`CoachFirstMeet`、`GroundZero_Intro`

> 结论：**「新手引导」= `Level_Guide` 关卡（3 个子场景）+ `Guide_Intro*` 三段过场**，
> 序章 `Prologue` 与捏脸是独立的一步。

## 待确认（需要跑一次诊断）

1. `GameplayDataSettings.SceneManagement.PrologueScene` 具体指向哪个场景（资源里的字段，需 dump 或运行时打印）
2. `Level_Guide` 到底由谁触发进入：序章末尾的脚本、基地里的教练 NPC（`CoachFirstMeet`），还是任务
3. 跳过引导后是否需要补发：初始任务链、初始物品、基地建筑、`CreatedWithVersion`
4. 跳过序章会不会连带跳过捏脸（`DreamCustomFace`）——若是，则**不能**跳过序章

## 诊断用法

安装本 mod 后新建一个存档并正常玩到进基地，然后查看游戏根目录：

```
E:\Steam\steamapps\common\Escape from Duckov\SkipTutorial.log
```

日志会按顺序记录：

```
[信息] 场景已加载：MainMenu（Single，buildIndex=1）
[信息] 场景已加载：Prologue_Main（Single，buildIndex=...）
[信息] 过场已播放：DreamCustomFace
...
[信息] 场景已加载：Level_Guide_Main（...）
[信息] 关卡初始化完成：IsRaidMap=False，IsBaseLevel=False
[信息] 场景已加载：Base（...）
[信息] 关卡初始化完成：IsRaidMap=False，IsBaseLevel=True
```

靠这份顺序就能确定「引导是何时、被谁触发的」。

## 当前实现（2026-10-22）

| 项 | 做法 |
| --- | --- |
| 跳过范围 | **只跳 `Level_Guide`**；序章、捏脸、`Prologue` 剧情全部保留 |
| 拦截点 | `SceneLoader.LoadScene(SceneReference, ...)` 前缀：目标是 `Level_Guide*` 时把参数换成 `SceneManagement.BaseScene`，异步等待链不变 |
| 生效条件 | 当前存档 `MultiSceneCore_Visited_Level_Guide_Main == false`（即还没走过引导） |
| 补写标记 | `MultiSceneCore.SetVisited("Level_Guide_Main/_1/_2")` + `SavesSystem.Save("CutScene_Guide_Intro/Intro2/IntroDodge", true)` |
| 启动资金 | 到达基地后 `EconomyManager.Add(3000)`（现金账户，与任务奖励金钱同一接口） |
| 只发一次 | 存档键 `SkipTutorial_RewardGranted`；发放失败会在下一次关卡初始化重试 |

### 验证方法

新建存档 → 捏完脸、序章剧情结束 → **应当直接进入基地**，日志出现：

```
[信息] 已跳过新手引导：Level_Guide_Main → 基地
[信息] 场景已加载：Base（...）
[信息] 关卡初始化完成：IsRaidMap=False，IsBaseLevel=True
[信息] 已发放新手启动资金 3000
```

如果日志里**没有**「已跳过新手引导」，说明引导不是走 `SceneLoader.LoadScene` 加载的，
把 `SkipTutorial.log` 发出来，按实际加载路径补钩子。

### 已知待验证

- 跳过引导后，基地的初始建筑 / 初始任务 / 初始物品是否由基地场景自行初始化（若缺失需要补）
- 引导关是否会给某些全局解锁（`EconomyManager.Unlock` 等），需要时在跳过时补发

## 更新日志

完整版本记录见 [CHANGELOG.md](CHANGELOG.md)。

## 许可证

本项目以 [MIT 协议](LICENSE) 开源，可自由使用、修改与再发布，保留版权声明即可。

## 致谢

- 游戏与 mod 框架：TeamSoda《逃离鸭科夫》
- Harmony 补丁框架：[Harmony](https://github.com/pardeike/Harmony)（MIT）

- 感谢所有在创意工坊反馈问题与建议的玩家
