# Unity_2D_demo

一个用于学习、验证和沉淀 Unity 2D 游戏玩法与通用框架能力的工程。仓库同时包含可持续演进的 Framework、较完整的 Survivor 玩法，以及 FrogAdventure、RPG、Melee 等学习/原型场景；不同目录的成熟度和架构约束并不相同。

## 环境与启动

- Unity：`2022.3.62f2c1`
- 语言：C#
- 主要 UI：UGUI + TextMesh Pro
- 资源：`AssetLoader` 统一入口，当前为 Resources 与本地 Addressables 并存的迁移状态
- 网络：NativeWebSocket + Google.Protobuf，客户端 Socket 默认关闭

推荐用指定 Unity 版本打开仓库，并从 `Assets/Scenes/Launcher.unity` 进入主流程。`Launcher` 会创建常驻 `GameMgr`/`UILauncher`，初始化业务模块并展示 Survivor Home；从这里进入 `SurvivorsDemo` 才能覆盖完整的模块、UI 和场景切换生命周期。

Build Settings 当前启用：

```text
FrogAdventure/StartGame → FrogPrince → FrogPrince2 → EndGame
Launcher → SurvivorsDemo
```

`MeleeDemo.unity`、`RPGDemo.unity` 和 XLua 教程场景存在于仓库，但不在当前 Build Settings 中。

## 项目结构

```text
Assets/
├── Scenes/                       # 可运行场景
├── Scripts/
│   ├── Define/                   # 事件、模块、ViewType、协议等共享定义
│   ├── Framework/                # 启动、MVC、网络、资源、对象池、UI 等基础设施
│   ├── Modules/                  # FrogAdventure、Melee、RPG、Survivor 玩法
│   └── TestCode/                 # 学习与手工验证入口，不等同于自动化测试
├── Prefabs/、Resources/          # Prefab 与尚未迁移的 Resources 资源
├── AddressableAssetsData/        # 本地 Addressables 配置
├── Editor/                       # 自定义 Inspector 与 UI 创建菜单
└── XLua/                         # 第三方 XLua 源码、示例与文档
Docs/                             # 项目级架构、资源和模块文档
```

## 当前能力

### 通用 Framework

- `GameMgr` 统一驱动 Module、Timer、Pool、Network 和资源释放生命周期。
- Module/MVC 框架提供 `ModuleManager`、`BaseModule`、Proxy、Command、Presenter/View 与按 owner 清理的 EventBus。
- UIManager 负责 UI 层级、Prefab 加载、Presenter 缓存和窗口生命周期。
- `VirtualList` 支持固定尺寸的纵向、横向和网格虚拟列表；`VariableHeightVirtualList` 支持纵向异高、多模板、选择恢复和定位滚动。
- `PoolManager` 提供普通 GameObject 池和独立 UI 池，并支持预热、缓存上限和空闲缩容。
- `AssetLoader` 隔离 Resources/Addressables 后端；已迁移的 Survivor UI、通用弹窗和公共音频使用本地 Addressables Group。
- Network 提供 WebSocket 连接、Packet/Proto 编解码、消息分发、重连、发送结果和单响应等待。

### 玩法模块

| 模块 | 当前定位 | 入口/说明 |
| --- | --- | --- |
| Survivor | 当前主要演进模块；已闭合 Home、战斗、升级、Wave、掉落、GameOver 与重开 | `Launcher.unity`，详见 [Survivor 模块文档](Docs/Modules/Survivor.md) |
| FrogAdventure | 独立的关卡式学习玩法，仍主要由场景组件与 EventBus 协作 | `Assets/Scenes/FrogAdventure/` |
| RPG | 移动、近战、射击、敌人和背包/掉落实验代码，未接入 Module/MVC | `RPGDemo.unity`，详见 [RPG README](Assets/Scripts/Modules/Rpg/README.md) |
| Melee | 轻量近战移动、追击和生命原型 | `MeleeDemo.unity` |
| Login/TestCode | Module/Proxy/Command、加载、对象池和虚拟列表示例 | `Assets/Scripts/TestCode/` |

“存在代码或场景”不等于已经达到正式玩法、自动化测试或发布质量。当前明确的后续工作统一记录在 [BACKLOG.md](BACKLOG.md)。

## 架构与开发入口

开始修改前按任务选择事实源：

- 项目启动、Module、UI、Network 或跨模块关系：[Docs/Architecture.md](Docs/Architecture.md)
- 资源加载、Addressables 与释放边界：[Docs/ResourceManagement.md](Docs/ResourceManagement.md)
- Survivor 业务：[Docs/Modules/Survivor.md](Docs/Modules/Survivor.md) 及同目录专题文档
- MVC API：[Assets/Scripts/Framework/MVC/README.md](Assets/Scripts/Framework/MVC/README.md)
- Network API 与限制：[Assets/Scripts/Framework/Network/README.md](Assets/Scripts/Framework/Network/README.md)
- 第三方来源与许可证状态：[Docs/ThirdPartyAssets.md](Docs/ThirdPartyAssets.md)
- AI/Codex 修改规则：[AGENTS.md](AGENTS.md)

判断“当前已经实现什么”时，始终以当前分支代码、场景/Prefab 配置和 `ProjectSettings/ProjectVersion.txt` 为准；规划文档不能替代代码事实。

## 验证现状

仓库当前没有正式的 Edit Mode / Play Mode 自动化测试程序集。`Assets/Scripts/TestCode` 是手工演示代码，Survivor 的现有验收证据是 [Play Mode 验收记录](Docs/Modules/PlayModeAcceptance.md)。修改后至少应：

1. 检查 `git diff`，避免场景、Prefab、`.meta` 或资源的无关变化。
2. 在 Unity Console 确认无新增编译错误。
3. 涉及场景、UI、对象池或时间缩放时，从 `Launcher.unity` 执行对应 Play Mode 流程。
4. 无法运行 Unity Editor 时，只汇报已经完成的静态检查，不声称 Play Mode 已通过。

## Codex Skill

项目内的 `unity-mvc-development` Skill 位于 `.codex/skills/` 并随 Git 提交。首次在新电脑克隆后，可在项目根目录执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Install-CodexSkills.ps1
```

只预览安装行为：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Install-CodexSkills.ps1 -WhatIf
```

该 Skill 只适用于 Module/Proxy/Command/Presenter 风格的业务开发，不应套用于纯场景布局、美术资源、Shader 或独立原型脚本。
