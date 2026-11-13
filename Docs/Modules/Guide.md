# Unity Guide System — 第一阶段开发需求文档

## 1. 项目背景

基于 Unity + C# 实现一套通用游戏引导系统（Guide System）。

系统主要用于 MMO、RPG 游戏的新手引导，也需要具备后续扩展到其他游戏类型的能力。

当前实现支持按步骤点击 UI 目标推进、目标区域镂空遮罩和可选点击手势；复杂游戏操作检测仍不在范围内。

### 核心目标

1. 引导系统作为独立 Module，由现有 ModuleManager 管理。
2. 使用 ScriptableObject 配置引导内容。
3. 使用 GuideRunner 管理引导执行流程。
4. 使用 GuideView 展示引导 UI。
5. 支持开始、下一步、暂停、恢复、停止和完成。
6. 引导流程与 UI 展示解耦。
7. 支持通过 GuideCursor 或 PresenterId/TargetPath 两种方式注册 UI 点击目标，并允许每一步独立显示遮罩和手势。

---

## 2. 开发前置要求

在编写任何代码之前，必须先检查当前 Unity 项目的架构。

重点检查：

- BaseModule 的定义及生命周期。
- ModuleManager 的模块注册、初始化、获取和销毁方式。
- 当前项目的命名空间规范。
- 当前项目的 UI 管理方式。
- 当前项目的 Prefab 创建及加载方式。
- 当前项目是否已有事件系统。
- 当前项目是否已有通用 UI Presenter / View 结构。

### 特别要求

1. 必须复用现有 BaseModule 和 ModuleManager。
2. 不允许自行重新设计 ModuleManager。
3. 不允许假设存在某个方法后直接调用，必须以实际源码为准。
4. 不允许为了 Guide System 修改无关的基础架构。
5. 如果现有架构与本文档示例不一致，应优先适配现有架构。
6. 如果发现关键接口无法确定，先汇报问题和建议，不要擅自重构。

---

## 3. 系统架构

本阶段采用以下架构：

```text
ModuleManager
    |
    +-- GuideModule
            |
            +-- GuideRunner
            |
            +-- GuidePresenter
            |       |
            |       +-- GuideView
            |
            +-- GuideConfig
```

各模块职责：

| 类            | 类型             | 职责                       |
| ------------- | ---------------- | -------------------------- |
| GuideModule   | BaseModule       | 引导模块入口，对外提供 API |
| GuideRunner   | 普通 C#          | 管理引导执行流程           |
| GuidePresenter | BasePresenter<GuideView> | 管理引导界面的打开、关闭与交互 |
| GuideConfig    | ScriptableObject         | 保存一套引导配置               |
| GuideStepConfig | Serializable             | 保存单个引导步骤               |
| GuideView      | UIView                    | 展示引导 UI                    |
| GuideState     | Enum                      | 定义引导状态                   |

### 架构原则

- GuideModule 不负责 UI 具体展示。
- GuideRunner 不依赖 MonoBehaviour。
- GuideView 不负责引导流程判断。
- GuideConfig 只保存配置数据。
- 模块之间尽量通过明确的接口和事件通信。

---

## 4. 目录结构

优先遵循现有项目目录规范。如果项目没有对应目录，可以采用：

```text
Assets/
└── Scripts/
    └── Guide/
        ├── Core/
        │   ├── GuideModule.cs
        │   ├── GuideRunner.cs
        │   └── GuideState.cs
        │
        ├── Config/
        │   ├── GuideConfig.cs
        │   └── GuideStepConfig.cs
        │
        └── View/
            └── GuideView.cs

Assets/
└── GameData/
    └── Guide/
        └── Guide_001.asset
```

不要提前创建第二阶段及以后才需要的类。

---

## 5. GuideState

定义引导状态：

```csharp
public enum GuideState
{
    None,
    Running,
    Paused,
    Completed
}
```

状态说明：

| 状态      | 说明               |
| --------- | ------------------ |
| None      | 没有正在运行的引导 |
| Running   | 正在执行引导       |
| Paused    | 引导暂停           |
| Completed | 整套引导已经完成   |

状态转换要求：

```text
None
  |
  v
Running
  |
  +------> Paused
  |          |
  |          v
  |        Running
  |
  v
Completed
```

Stop 操作可以从 Running 或 Paused 返回 None。

Stop 不等于 Completed。

---

## 6. GuideStepConfig

用于定义单个引导步骤。

要求：

- 可序列化。
- 支持 Inspector 编辑。
- 包含唯一业务步骤 ID。
- 包含标题。
- 包含引导内容。
- 引导内容支持多行文本。

参考结构：

```csharp
[Serializable]
public class GuideStepConfig
{
    public int StepId;

    public string Title;

    [TextArea(2, 5)]
    public string Content;

    // 二选一：旧式 GuideCursor 目标标识
    public string TargetId;

    // 二选一：Presenter 编码与相对 View 路径
    public string PresenterId;
    public string TargetPath;

    public bool ShowMask = true;

    public bool ShowCursor = true;
}
```

注意：

StepId 是业务标识，不是数组索引。

不要要求 StepId 必须连续。

每一步必须且只能配置一种目标：

- 旧方式：配置非空 TargetId，并在目标上挂唯一匹配的 GuideCursor。
- 路径方式：配置 PresenterId 和 TargetPath；PresenterId 使用四位 ModuleName 编号、下划线和一位或两位 ViewType 编号，TargetPath 相对于 Presenter View 根节点，末端对象需挂 UIButton 或 Unity Button。

ShowMask 与 ShowCursor 分别控制遮罩和手势；Content 非空时显示在目标/手势附近，空内容时不显示气泡。

---

## 7. GuideConfig

使用 ScriptableObject 保存一整套引导配置。

要求：

- 支持 Unity CreateAssetMenu。
- 一个配置代表一套完整引导。
- 支持配置 GuideId。
- 支持配置 GuideName。
- 支持配置多个 GuideStepConfig。
- 可以在 Inspector 中调整步骤顺序。

参考结构：

```csharp
[CreateAssetMenu(
    fileName = "GuideConfig",
    menuName = "Game/Guide/Guide Config")]
public class GuideConfig : ScriptableObject
{
    public int GuideId;

    public string GuideName;

    public List<GuideStepConfig> Steps;
}
```

配置示例：

```text
GuideId: 1001
GuideName: 新手入门

Steps:
    StepId: 1
    Title: 欢迎进入游戏
    Content: 欢迎来到冒险世界！
    TargetId: NextButton
    ShowMask: true
    ShowCursor: true

    StepId: 2
    Title: 学习移动
    Content: 使用移动摇杆控制角色移动。
    TargetId: MoveStick
    ShowMask: true
    ShowCursor: true

    StepId: 3
    Title: 学习背包
    Content: 点击背包，查看你的装备。
    TargetId: Backpack
    ShowMask: true
    ShowCursor: true
```

---

## 8. GuideRunner

GuideRunner 是引导系统的核心执行器。

要求：

- 普通 C# 类。
- 不继承 MonoBehaviour。
- 不依赖 Unity UI。
- 负责管理当前引导配置。
- 负责管理当前步骤索引。
- 负责状态切换。
- 通过事件通知外部步骤和状态变化。

### 8.1 属性

需要提供：

```csharp
GuideState State { get; }

GuideConfig Config { get; }

int CurrentIndex { get; }

GuideStepConfig CurrentStep { get; }
```

CurrentIndex 从 0 开始。

CurrentStep 根据 Config 和 CurrentIndex 获取。

### 8.2 事件

需要提供：

```csharp
event Action<GuideStepConfig, int, int> OnStepChanged;

event Action<GuideState> OnStateChanged;

event Action OnCompleted;
```

OnStepChanged 参数：

- 当前步骤数据。
- 当前步骤索引。
- 总步骤数量。

### 8.3 对外方法

```csharp
bool Start(GuideConfig config);

void Next();

void Pause();

void Resume();

void Stop();
```

### 8.4 Start

要求：

1. 检查配置是否为空。
2. 检查 Steps 是否为空。
3. 检查步骤数量是否大于 0。
4. 如果已有引导正在运行，先清理旧流程。
5. 设置当前配置。
6. CurrentIndex 设置为 0。
7. 状态切换为 Running。
8. 触发 OnStepChanged。

异常配置不得导致空引用。

### 8.5 Next

要求：

1. 只有 Running 状态允许执行。
2. CurrentIndex 加 1。
3. 如果还有下一步，触发 OnStepChanged。
4. 如果已经没有下一步，执行 Complete。

Paused 状态下调用 Next 不应该推进步骤。

### 8.6 Pause

只有 Running 状态允许暂停。

暂停后：

- State 变成 Paused。
- 当前步骤不变。
- 不清除配置。
- 不触发下一步事件。

### 8.7 Resume

只有 Paused 状态允许恢复。

恢复后：

- State 变成 Running。
- 当前步骤不变。
- 不重新触发 OnStepChanged，避免 UI 重复初始化。

### 8.8 Stop

要求：

- 清除当前配置。
- CurrentIndex 设置为 -1。
- 状态变为 None。
- 不触发 OnCompleted。

### 8.9 Complete

要求：

- 清除当前配置。
- CurrentIndex 设置为 -1。
- 状态变成 Completed。
- 触发 OnCompleted。
- 不允许重复完成。

### 8.10 边界情况

需要处理：

- Start(null)。
- 空步骤配置。
- Next 连续调用。
- Paused 状态下 Next。
- None 状态下 Next。
- Completed 状态下 Next。
- 重复 Pause。
- 重复 Resume。
- Stop 后再次 Start。
- 运行中使用新配置重新开始。

---

## 9. GuideModule

GuideModule 继承项目现有 BaseModule。

负责：

1. 创建 GuideRunner。
2. 提供引导系统统一 API。
3. 管理 GuideRunner 生命周期。
4. 在模块关闭时清理引导。

对外提供：

```csharp
Task<bool> StartGuide(GuideConfig config);

void Next();

void Pause();

void Resume();

void Stop();
```

要求：

- 使用项目实际的 BaseModule 生命周期方法。
- 在模块初始化时创建 GuideRunner。
- 在模块关闭时停止引导并释放引用。
- 不允许 GuideModule 直接操作 Unity UI。
- 不允许其他业务模块绕过 GuideModule 直接控制流程。

---

## 10. GuideView

GuideView 继承项目的 UIView。

负责展示当前引导步骤。

### 10.1 Prefab UI 结构

```text
GuidePanel (UIView)
├── GuideMask
│   ├── Top
│   ├── Right
│   ├── Bottom
│   └── Left
├── GuideTips
│   └── tmp
└── GuideCursor
```

`GuidePresenter` 通过 UIManager 加载 `Assets/Prefabs/Guide/GuidePanel.prefab`，并放入 `UILayerIndex.Tip`。Prefab 的 GuideView 直接复用四片遮罩、手势和文本控件；不在运行时另建第二套遮罩或手势对象。GuideCursor 可添加到带 UI Graphic 的目标，不要求目标使用 Unity Button。也可在 GuideStepConfig 中使用 PresenterId 和 TargetPath，通过 GuidePathFinder 查找已有 Presenter View 下的 UIButton 或 Unity Button，无需为该目标添加 GuideCursor；该路径方式依赖 Prefab 的层级名称。

### 10.2 UI 展示

GuideModule 启动流程后异步打开 GuidePresenter，Presenter 根据 Runner 当前步骤更新 View：

- 在 Tip 层显示 GuidePanel，并置于该层最上方。
- 每一步使用 GuideCursor/TargetId 或 PresenterId/TargetPath 其中一种方式解析点击目标并推进。
- ShowMask 与 ShowCursor 独立控制遮罩和手势。
- Content 非空时，在目标/手势附近显示说明；空内容时隐藏说明区域。
- 遮罩镂空区域随目标 RectTransform 的布局、缩放和动画变化更新。

### 10.3 状态变化

| 状态      | UI 行为              |
| --------- | -------------------- |
| Running   | 显示目标交互与配置的引导内容 |
| Paused    | 清除目标交互并隐藏引导层 |
| None      | 隐藏并缓存 Presenter |
| Completed | 隐藏并缓存 Presenter |

### 10.4 目标交互

GuideCursor 标记可点击的 UI 目标，不依赖 Unity Button。目标 Graphic 必须启用 Raycast Target；也可以通过 GuideCursor 配置更精确的点击/高亮矩形。另一种方式通过 PresenterId 和相对 View 根节点的 TargetPath 查找 UIButton 或 Unity Button。点击任一目标：

```csharp
guideModule.Next();
```

GuideView 不直接修改 CurrentIndex。

### 10.5 生命周期

- GuidePresenter 在 OnOpen 订阅 Runner 与目标，在 OnClose 解除订阅。
- 引导结束、停止或暂停时隐藏目标交互层。
- GuideDemo 通过 GuideModule.StartGuide 异步打开 Presenter，不再绑定场景中的 GuideView。

---

## 11. ModuleManager 集成

必须检查现有 ModuleManager 实现。

要求：

1. 将 GuideModule 注册到现有模块管理体系。
2. 确保 GuideModule.Init() 正常执行。
3. 在场景 UI 初始化完成后获取 GuideModule。
4. 调用并等待 GuideModule.StartGuide(config)。
5. UIManager 根据 GuidePresenter 将 GuidePanel 放到 Tip 层。
6. 不修改 ModuleManager 核心实现，除非确实存在必要且经过说明的改动。

启动流程：

```text
游戏启动
    |
    v
ModuleManager 初始化
    |
    v
GuideModule 初始化
    |
    v
UILauncher 初始化 TipLayer
    |
    v
GuideModule.StartGuide(config)
    |
    v
GuideRunner 通知 GuidePresenter
    |
    v
UIManager 在 TipLayer 打开 GuidePanel
```

以上为逻辑流程；调用 StartGuide 前，UIManager 必须已由 UILauncher 初始化。

---

## 12. 测试配置

创建一套测试配置：

```text
GuideId: 1001
GuideName: 新手入门

Step 1:
    Title: 欢迎进入游戏
    Content: 欢迎来到冒险世界，开始你的旅程吧！

Step 2:
    Title: 学习移动
    Content: 使用移动摇杆控制你的角色移动。

Step 3:
    Title: 学习背包
    Content: 点击背包，查看你的装备。
```

在测试场景中：

1. 获取 GuideModule。
2. 绑定 GuideView。
3. 启动 GuideConfig。
4. 点击下一步。
5. 验证步骤切换。
6. 验证完成后 UI 隐藏。

---

## 13. 验收标准

以下项目必须全部通过。

### 基础流程

- [ ] GuideModule 正常注册。
- [ ] GuideModule 正常初始化。
- [ ] GuideRunner 正常创建。
- [ ] GuideConfig 可以通过 Inspector 创建。
- [ ] GuideView 正常绑定。
- [ ] StartGuide 可以开始引导。
- [ ] 第一条引导正确显示。
- [ ] 点击下一步可以进入第二条。
- [ ] 最后一步显示完成按钮。
- [ ] 完成后 UI 自动隐藏。

### 状态控制

- [ ] Pause 可以暂停。
- [ ] 暂停后不能点击下一步。
- [ ] Resume 可以恢复。
- [ ] Stop 可以停止。
- [ ] Stop 不会触发 OnCompleted。
- [ ] Completed 状态不会重复触发。

### 异常处理

- [ ] 空配置不会报空引用异常。
- [ ] 空步骤不会报空引用异常。
- [ ] 重复点击不会导致越界。
- [ ] 重复 Bind 不会造成事件重复。
- [ ] Unbind 后不会继续接收事件。
- [ ] Stop 后可以重新开始另一套引导。

---

## 14. 本阶段明确不实现的内容

以下内容留到后续阶段：

### 第二阶段：目标注册与 UI 高亮

- IGuideTarget。
- GuideTargetRegistry。
- GuideUITarget。
- GuideWorldTarget。
- GuideMask。
- Shader 镂空。
- 动态目标位置跟随。
- GuidePointer 手指动画。

### 第三阶段：引导完成条件

- 点击指定目标才完成。
- 游戏事件监听。
- 自定义条件判断。
- 移动、攻击、拾取等操作检测。
- 业务事件驱动步骤切换。

### 第四阶段：输入控制

- 输入屏蔽。
- 目标区域输入放行。
- 世界操作控制。
- 引导跳过。

### 第五阶段：持久化

- GuideProgress。
- 本地进度保存。
- 服务器进度同步。
- 中断恢复。
- 多条引导管理。

不要提前实现上述功能。

---

## 15. AI 开发执行要求

请按照以下顺序执行：

### 第一步：分析现有项目

检查：

- BaseModule。
- ModuleManager。
- 模块初始化流程。
- UI 管理流程。
- 场景生命周期。
- 项目命名空间和代码规范。

先输出简短的架构分析和计划。

### 第二步：实现基础数据结构

实现：

- GuideState。
- GuideStepConfig。
- GuideConfig。

### 第三步：实现流程控制

实现：

- GuideRunner。
- 状态切换。
- 步骤切换。
- 事件通知。
- 边界处理。

### 第四步：接入模块体系

实现：

- GuideModule。
- ModuleManager 注册。
- 模块生命周期管理。

### 第五步：实现 UI

实现：

- GuideView。
- UI 事件绑定。
- 状态刷新。
- 事件解绑。

### 第六步：完成测试

创建测试配置及测试场景接入。

检查所有验收标准。

### 第七步：输出开发报告

报告必须包含：

1. 新增文件列表。
2. 修改文件列表。
3. 每个类的职责。
4. 类之间的调用关系。
5. Unity Inspector 配置步骤。
6. 测试方法。
7. 验收结果。
8. 尚未解决的问题。

### 重要限制

- 不要修改无关代码。
- 不要擅自重构现有架构。
- 不要引入第三方依赖。
- 不要为了本功能新增不必要的 Manager。
- 不要将业务流程写入 GuideView。
- 不要将 UI 逻辑写入 GuideRunner。
- 不要提前实现后续阶段功能。
- 所有代码必须能够在项目当前 Unity 版本下编译。

**最终目标：完成一套可运行、可配置、可暂停恢复、可由 ModuleManager 管理的基础 Guide System。**
