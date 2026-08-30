# Unity_2D_demo 架构说明

> 本文记录仓库**当前可验证的实现**与明确的演进约束，作为 ChatGPT / Codex 理解项目时的项目级事实源。
>
> 若本文与代码冲突，以当前分支代码和 `ProjectSettings/ProjectVersion.txt` 为准；架构发生变化时，应在同一次变更中同步更新本文。

## 1. 项目定位

- 引擎：Unity `2022.3.62f2c1`
- 语言：C#
- 项目性质：用于持续学习、验证和沉淀 Unity 2D 游戏能力及通用游戏框架的工程。
- 当前仓库同时包含框架代码与多个玩法/示例模块，因此不要假设所有 `Assets/Scripts/Modules` 下的代码都使用同一成熟度或同一套架构。

## 2. 架构事实源优先级

AI 或开发者判断“项目现在是什么样”时，按以下顺序取证：

1. 当前分支中的实际代码与 Unity 配置。
2. 本文 `Docs/Architecture.md`。
3. `Assets/Scripts/Framework/**/README.md` 等局部框架文档。
4. `.codex/skills/**` 中的任务型操作规范。
5. `AGENTS.md` 中的开发约束与工程规则。
6. issue、聊天记录、TODO 和口头计划。

计划中的类、目录或架构不能仅因为出现在文档或聊天中，就当作已经存在；修改前必须搜索仓库确认。

## 3. 当前顶层代码组织

当前核心代码主要分为两类：

```text
Assets/Scripts/
├── Framework/                 # 通用框架与基础设施
│   ├── Launcher/              # 游戏启动与全局生命周期编排
│   ├── MVC/                   # Module / Proxy / Command / Presenter / UIManager
│   └── Network/               # 网络、Packet、Proto、消息分发
├── Modules/                   # 具体玩法或业务模块
│   ├── Misc/                  # 通用杂项业务/提示类功能
│   ├── FrogAdventure/         # 青蛙冒险相关玩法
│   └── Vampire Survivors-like/# 类幸存者玩法
└── TestCode/                  # 测试、学习和验证代码
```

目录是职责提示，不等于强制依赖边界。判断依赖时仍需查看具体调用关系。

## 4. 启动与全局生命周期

当前全局启动入口为：

```text
Assets/Scripts/Framework/Launcher/GameMgr.cs
```

`GameMgr` 当前职责：

```text
Awake
  ├─ 建立单例
  ├─ DontDestroyOnLoad
  └─ InitializeModules
       ├─ PushModules<MiscModule>()
       ├─ PushModules<LoginModule>()
       └─ ModuleManager.InitializeAll()

Start
  └─ NetworkMgr.Connect(...)

Update
  ├─ TimerManager.OnUpdate()
  ├─ PoolManager.OnUpdate()
  └─ ModuleManager.Update()

OnDestroy
  ├─ ModuleManager.ReleaseAll()
  └─ NetworkMgr.Dispose()
```

因此，新增“全局常驻系统”前应先判断它属于：

- `GameMgr` 的生命周期编排；
- 某个独立 Manager/Service；
- 某个业务 Module；
- 或仅属于单一场景/玩法。

不要把具体业务逻辑继续堆入 `GameMgr`。

## 5. Module / MVC 业务框架

核心目录：

```text
Assets/Scripts/Framework/MVC/
```

当前框架采用按业务域组织的 Module + MVP/MVC 风格职责划分：

```text
ModuleManager
    ↓
 BaseModule
    ├─ Command
    ├─ Proxy
    └─ Presenter
          ↓
         View
```

### ModuleManager

负责：

- Module 注册和创建；
- 批量初始化；
- Update 驱动；
- Module 释放。

它是 Module 系统的统一入口，不承载具体业务规则。

### BaseModule

每个业务模块通过唯一 `ModuleName` 标识，并聚合本模块的 Command、Proxy 与 Presenter。

原则：

- `OnInit` 做模块内部对象登记和初始化；
- `OnRelease` 负责对应生命周期收口；
- 模块内部组件优先通过模块自身获取；
- 跨模块访问必须显式经过 `ModuleManager` 或稳定接口/事件边界；
- 禁止形成循环依赖。

Presenter 采用定义于 `Assets/Scripts/Define/ViewType.cs` 的模块 ViewType 映射：模块在 `OnInit` 中通过
`RegPresenter<TPresenter>(viewType, prefabPath, layer)` 登记 ViewType 与 Presenter、Prefab 的一一对应关系，调用 `OpenWindow<TPresenter>(viewType, args)` 时才实例化并缓存界面。一个 Presenter 类型应只归属一个 Module，并只绑定一个 ViewType；`BaseModule` 负责本模块内的重复注册校验。`BaseModule` 与 `UIManager` 统一以 `ModuleViewKey`（`ModuleName + ViewType`）作为 Presenter 缓存身份。ViewType 命名采用 `模块名ViewType`，例如 `SurvivorViewType` 与 `MiscViewType`。
已打开的 Presenter 仅可通过 `GetPresenter(viewType)` 按 ViewType 查询，不提供按 Presenter 类型查询的 Module API。

### Proxy

负责：

- 模块业务数据与状态；
- 网络协议注册和协议回调；
- 数据层面的业务操作。

不应直接操作 UI。

### Command

负责一次明确的业务动作或业务流程编排。

Command 不自行成为长期事件中心，不应承担持久化数据容器，也不要直接处理底层网络协议细节。

### Presenter / View

Presenter 负责界面生命周期、交互协调和展示逻辑；View 负责 Unity 组件引用与显示。

业务 UI 的目标调用方向为：

```text
UIManager -> Presenter -> View
                    ↓
             Module / Proxy / Command
```

网络回调不要直接反向操作 View。

## 6. UI 系统现状

核心实现：

```text
Assets/Scripts/Framework/MVC/UIManager.cs
```

`UIManager` 当前是纯 C# 单例，已经包含：

- UI 层级配置；
- Resources Prefab 加载；
- GameObject 缓存；
- Presenter 创建、缓存和生命周期调用；
- Window 打开/关闭；
- Model 层弹窗栈。

当前层级枚举为：

```text
Main
Window
Model
Tip
```

需要注意：

1. 当前加载实现仍直接使用 `Resources.Load`，代码注释已为 Addressables / AssetBundle 等资源方案预留替换空间；不要把“未来资源系统”描述成当前已实现。
2. `CloseWindow` 当前存在立即 `OnDestroy` 的行为以及延迟关闭 TODO，因此不要假设所有关闭窗口都会进入长期缓存。
3. 修改 UI 层级、粒子、红点等渲染顺序时，优先遵循现有 Canvas/父子层级模型，而不是无限放大 `sortingOrder`。

## 7. Network / Protobuf 现状

核心目录：

```text
Assets/Scripts/Framework/Network/
```

仓库中当前可验证的核心组成包括：

```text
NetworkMgr.cs
PacketCodec.cs
ProtoMgr.cs
ProtoRegister.cs
README.md
```

职责方向保持为：

```text
连接管理
   ↓
Packet 编解码
   ↓
Proto 编解码 / 注册
   ↓
消息分发
   ↓
Proxy / Module 业务处理
   ↓
Presenter / UI
```

约束：

- 底层网络层不直接操作 UI；
- 协议处理优先落到 Proxy/业务层；
- `cmd` 类型在整个协议链路保持统一；
- Proto 映射/注册优先自动化，避免业务协议越来越多后维护大量手写注册代码。

## 8. 通用基础设施

`GameMgr` 当前明确驱动：

- `TimerManager`
- `PoolManager`
- `ModuleManager`
- `NetworkMgr`

这些属于项目级基础设施。新增 Manager 前先检查是否已有同职责实现，避免产生：

```text
XXXManager
XXXMgr
XXXService
```

三套重复系统。

## 9. Codex Skill

仓库已存在：

```text
.codex/skills/unity-mvc-development/SKILL.md
```

它用于“新增或修改 MVC 风格业务模块”这类任务，不是所有 Unity 工作的万能 Skill。

执行 MVC 业务任务时推荐阅读顺序：

```text
AGENTS.md
   ↓
Docs/Architecture.md
   ↓
.codex/skills/unity-mvc-development/SKILL.md
   ↓
目标 Module 与 Framework/MVC 基类
   ↓
真实调用方
```

对于 Shader、场景布局、纯美术资源、Editor 工具等非 MVC 工作，不应强行套用该 Skill。

## 10. 依赖设计原则

推荐依赖方向：

```text
Gameplay / Feature Module
        ↓
Business Abstraction / Framework API
        ↓
Framework Infrastructure
        ↓
Unity / Network / Resource implementation
```

重点规则：

- Framework 不反向依赖某个具体玩法 Module。
- View 不成为业务状态真源。
- Network 不依赖 UI。
- 跨 Module 协作优先稳定接口、事件或共享 Service。
- 不为了“解耦”滥用全局 EventBus；明确的一对一调用仍可使用接口。
- 遇到互相依赖时，先抽取共同契约或上移稳定能力，不允许用 Singleton 查找把循环依赖藏起来。

## 11. 当前架构的演进原则

以下是演进原则，不代表已经完成：

- UI 资源加载可逐步从 `Resources.Load` 抽象为统一资源层；
- Module 系统继续保持显式生命周期和依赖边界；
- Proto 注册继续向生成式/自动注册方向演进；
- 具体玩法逐步复用 Framework，而不是把玩法特例塞回 Framework；
- 新通用系统先在真实业务中验证，再沉淀为 Framework，避免提前设计“大而全”框架。

## 12. AI 修改项目时的检查表

修改前：

- 搜索目标类是否真实存在；
- 阅读调用方和生命周期；
- 判断修改属于 Framework 还是具体 Module；
- 判断文档描述的是“当前实现”还是“未来方向”。

修改后：

- 检查是否引入跨层反向依赖；
- 检查事件、Tween、协程、异步回调是否正确释放；
- 检查序列化字段和 Prefab/Scene 兼容；
- 检查是否误改 Unity `.meta`、场景或无关资源；
- 架构边界若发生变化，同步更新本文。

---

这份文档的目标不是把项目冻结成某种“完美架构”，而是让后续 ChatGPT、Codex 和开发者每次进入项目时，都先基于**仓库真实状态**继续演进，而不是根据上一次聊天记忆重新猜一遍项目。
