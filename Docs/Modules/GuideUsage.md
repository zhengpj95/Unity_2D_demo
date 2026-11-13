# Guide 引导系统使用说明

本文说明当前 Guide 模块中的文件职责、配置和场景接入方式，以及在实际业务中如何触发、取消和切换 Guide。目标高亮和两种目标定位方式已实现；等待业务操作结果后推进步骤、操作条件检测和已完成进度保存目前尚未实现。

## 1. 文件职责

| 文件                                            | 职责                                                                                            |
| ----------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| `Assets/Scripts/Modules/Guide/GuideState.cs`    | 定义 `None`、`Running`、`Paused`、`Completed` 四种状态。                                        |
| `Assets/Scripts/Modules/Guide/Config/GuideStepConfig.cs` | 定义可在 Inspector 编辑的步骤内容及两种互斥目标配置。`StepId` 是业务 ID，不是步骤索引。 |
| `Assets/Scripts/Modules/Guide/GuideConfig.cs`   | ScriptableObject 配置；一个资源代表一套引导，包含 `GuideId`、名称和有序步骤列表。               |
| `Assets/Scripts/Modules/Guide/GuideRunner.cs`   | 纯 C# 流程执行器；保存当前配置、状态和步骤索引，并发出步骤、状态及完成事件。                    |
| `Assets/Scripts/Modules/Guide/GuideModule.cs`   | 由 ModuleManager 管理的统一入口；创建和释放 Runner，并提供开始、下一步、暂停、恢复和停止 API。  |
| `Assets/Scripts/Modules/Guide/GuidePathFinder.cs` | 按 Presenter 标识和 View 相对路径查找 `UIButton` 或 Unity `Button`。                              |
| `Assets/Scripts/Modules/Guide/GuideCursor.cs`    | 旧式目标标记；按 `TargetId` 匹配并提供点击通知与可高亮区域。                                    |
| `Assets/Scripts/Modules/Guide/View/GuideView.cs` | 显示手势、说明和遮罩；步骤与目标协调由 GuidePresenter 负责。                                      |
| `Assets/Scripts/TestCode/GuideDemoBootstrap.cs` | 按场景 Inspector 引用启动指定配置的演示组件；GuideDemo 与 Launcher 的样例入口使用。              |
| `Assets/Editor/GuideDemoSetup.cs`               | Editor 菜单工具，用于创建/重建 GuideDemo 演示场景。                                             |
| `Assets/Configs/Guide/Guide_001.asset`          | 三步新手入门演示配置。                                                                          |
| `Assets/Configs/Guide/Guide_002.asset`          | Launcher 中的两步弹窗引导配置，目标为 `btnTip` 和 `btn_confirm`。                              |

`Assets/Scripts/Framework/Launcher/GameMgr.cs` 在启动时注册 `GuideModule`。`GuidePresenter` 通过 UIManager 加载 `GuidePanel`，协调 Runner、View 和目标；目标可使用 GuideCursor 标记，也可通过 GuidePathFinder 按 Presenter/View 路径查找。

## 2. 创建引导配置

1. 在 Unity Project 窗口选择 **Create → Game Data → Guide → Guide Config**，创建 `GuideConfig` 资源。
2. 设置业务 `GuideId`（建议项目内唯一）和便于识别的 `GuideName`。
3. 按执行顺序填写 `Steps`。每步设置不重复的 `StepId`、`Title` 和 `Content`，并选择一种目标配置方式。
4. `StepId` 不要求连续；执行顺序由 Inspector 列表顺序决定。

`StartGuide` 会拒绝空配置、空步骤列表、空步骤项以及重复的 `StepId`，并返回 `false`。`GuideId` 目前只是配置上的业务标识，不会被模块用于查找、去重或记录完成状态。

### 两种目标配置方式

每个步骤必须且只能选择以下一种方式；不要同时填写或同时留空：

1. **旧方式：`TargetId`**。填写场景中 `GuideCursor` 的目标标识；目标需处于启用状态并具有可接收射线的 UI Graphic。
2. **路径方式：`PresenterId` + `TargetPath`**。`PresenterId` 格式为四位 `ModuleName` 枚举值、下划线、一位或两位模块 `ViewType` 枚举值。例如 `1002_2` 表示 `ModuleName.Misc` 的 `MiscViewType.Confirm`。`TargetPath` 是相对于该 Presenter 的 View 根节点的 Transform 路径，路径末端对象必须有 `UIButton` 或 Unity `Button` 组件。当前需要在 GuidePathFinder 中映射对应模块的 ViewType 枚举。

路径方式不要求在目标上添加 GuideCursor。目标 Presenter 尚未打开时，引导会等待对应的 Presenter 打开事件，再按路径查找；若已打开但路径或按钮组件配置错误，会记录错误日志。路径跟随 Prefab 层级，重命名或移动目标节点时要同步更新配置。

## 3. 先弄清楚：一个 Config 代表什么

**一个 `GuideConfig` 代表一段连续播放的引导内容，里面有多条按顺序显示的步骤。** 它不是一条步骤，也不是能自动判断整场游戏任务进度的总流程。

例如，`GuideConfig (GuideId=1001, 抽奖引导)` 可以包含：

| 列表顺序 | StepId | 当前系统能做的事                 |
| -------- | ------ | -------------------------------- |
| 1        | 10     | 显示“打开抽奖界面”的标题和说明。 |
| 2        | 20     | 显示“点击抽奖按钮”的标题和说明。 |
| 3        | 30     | 显示“抽奖完成”的标题和说明。     |

玩家每点一次 GuideView 的“下一步”，Runner 就显示下一条；最后再点一次就完成这套 Config。这里的 Step 目前只是文本展示，系统**不会**自动打开抽奖界面、监听抽奖按钮、判断抽奖成功或等待玩家操作完成。

你描述的“抽奖 → 回主界面 → 升级到 10 级 → 升星 → 回主界面 → 战斗”则是多个受游戏进度条件控制的阶段。建议拆成多份 Config：

| Config 示例     | 何时开始                                 | 何时结束/切换                                                                        |
| --------------- | ---------------------------------------- | ------------------------------------------------------------------------------------ |
| `1001 抽奖引导` | 新玩家进入流程且尚未完成抽奖引导时。     | 抽奖业务确认成功后，关闭抽奖界面/返回主界面；若产品定义为引导完成，则结束本 Config。 |
| `1002 升级引导` | 回到主界面并满足可进入升级流程的条件时。 | 实际等级达到 10 后，业务代码停止或完成当前引导，再触发下一阶段。                     |
| `1003 升星引导` | 等级达到 10，且升星功能可用时。          | 升星业务确认成功后，回主界面并触发下一阶段。                                         |
| `1004 战斗引导` | 已升星且满足进入战斗的业务条件时。       | 按战斗引导自身的业务规则结束。                                                       |

表中的“何时开始/结束”是游戏业务的决策，不是当前 GuideConfig 能配置的条件。`GuideId` 用来让开发者和业务进度系统识别是哪一段；**目前 `GuideModule` 不会按 ID 自动查找或排队**。持有对应 Config 引用的业务流程对象在条件满足时调用 `StartGuide(config)`，成功操作后调用 `Next()`、`Stop()` 或开始下一份 Config。

只有一个不受业务结果打断、从头到尾纯顺序播放的短引导，才适合做成一个包含全部文本步骤的 Config。遇到“等待抽奖成功”“等级达到 10”“升星成功”这类条件，建议按阶段拆 Config，并在业务操作成功的回调/状态变化处切换，而不是把条件伪装成一个 Step。

## 4. 在场景中显示引导

### 演示场景

1. 等待 Unity 编译完成。
2. 执行 **Tools → Guide → Create Guide Demo Scene**。
3. 打开 `Assets/Scenes/GuideDemo.unity` 并进入 Play Mode。
4. 场景中的 `GuideDemoBootstrap` 会取得 `ModuleName.Guide` 对应的 GuideModule 并启动 Inspector 指定的配置；`GuidePresenter` 负责加载和显示 `GuidePanel`。
5. 点击按钮依次切换步骤；最后一步按钮显示“完成”，再点击后进入 `Completed` 并隐藏面板。

如果演示场景对象或引用缺失，检查 `GameMgr`、`EventSystem`、`Main Camera`、`GuideView` 的 Inspector 字段以及 `GuideDemoBootstrap` 上的配置引用。重新执行菜单会重建演示场景；先保存当前场景中的其他工作。

### Launcher 场景中的 Guide_002

`Launcher.unity` 的根对象包含 `GameMgr`，由它初始化 `GuideModule`；`UIRoot` 上的 `GuideDemoBootstrap` 引用 `Guide_002` 并在场景启动后播放样例：先点击带有 `btnTip` 目标标识的按钮打开确认弹窗，再点击弹窗中带有 `btn_confirm` 标识的按钮完成引导。确认弹窗异步实例化时，GuidePresenter 会等待对应目标启用后再显示该步骤。

### 正式游戏场景

`Launcher.unity` 当前的 `GuideDemoBootstrap` 仅用于演示 `Guide_002` 的场景启动链路。正式业务引导不应依赖它判断玩家进度；应由实际拥有业务条件的 Presenter、场景控制器或玩法流程对象持有配置引用，并在条件满足时调用模块 API：

```csharp
private GuideModule _guideModule;

private void Start()
{
  _guideModule = ModuleManager.Instance.GetModule<GuideModule>(ModuleName.Guide);
  if (_guideModule == null)
  {
    Debug.LogError("[InventoryPresenter] GuideModule is not registered.");
    return;
  }
}

private void OnInventoryUnlocked()
{
  if (!_guideModule.StartGuide(inventoryGuideConfig))
    Debug.LogWarning("[InventoryPresenter] Inventory guide config is invalid.");
}
```

上例中的 `inventoryGuideConfig` 应作为序列化字段引用对应的 GuideConfig 资源，或由项目实际采用的资源加载流程提供。不要假设 `Assets/Configs/Guide` 下的普通 Asset 可以通过 `Resources.Load` 自动找到。

GuideView 必须在开始引导前 `Bind(guideModule)`。同一 Runner 的步骤/状态变化会通知已绑定的 View；GuideView 的 Next 按钮只会调用 `GuideModule.Next()`。场景销毁时调用 `Unbind()`，防止遗留事件订阅。正式游戏若需要显示 UI，应在场景初始化时将 GuideView 与模块绑定；是否常驻、在哪个 Canvas 下显示由游戏场景决定。

## 5. 运行时状态和 API

```csharp
guideModule.StartGuide(config); // bool：配置有效并开始时为 true
guideModule.Next();             // 仅 Running 时推进
guideModule.Pause();            // 仅 Running 时暂停
guideModule.Resume();           // 仅 Paused 时恢复
guideModule.Stop();             // 清除当前流程，不算完成
```

状态行为：

| 操作                                | 结果                                                                              |
| ----------------------------------- | --------------------------------------------------------------------------------- |
| `StartGuide(config)`                | 从列表第 0 项开始，状态为 `Running`，发送当前步骤事件。                           |
| `Next()` 且还有后续步骤             | 索引加一并显示下一步。                                                            |
| 最后一步上调用 `Next()`             | 状态变为 `Completed`，清除当前配置/索引并通知完成事件；GuideView 隐藏。           |
| `Pause()` / `Resume()`              | 保留当前步骤；恢复时只发状态事件，不重复初始化步骤 UI。                           |
| `Stop()`                            | 状态变为 `None`，当前配置清空、索引变为 -1，不发完成事件；绑定的 GuideView 隐藏。 |
| Running/Paused 时开始另一套有效配置 | 先停止旧流程，再从新配置第一步开始。旧流程不会被标记完成。                        |

流程状态可通过 `guideModule.Runner.State`、`Config`、`CurrentIndex` 和 `CurrentStep` 查询。引导运行时 `Config.GuideId` 可用于业务日志或判断当前活动配置；停止或完成后 `Config` 为 null，因此应在清理前读取 ID。不要用 `CurrentIndex` 代替业务 `StepId`。

## 6. 游戏操作如何取消当前 Guide 并触发下一个 GuideId

**触发和取消条件必须由业务流程决定。** 当前 Guide 系统没有目标点击检测、条件表达式、GuideId 调度表、完成记录或持久化；它也不会因为玩家完成了某个游戏操作而自动推进/取消。演示按钮只是手动调用 `Next()`。

推荐由负责操作结果的业务代码在操作成功后按顺序处理：

1. 确认业务操作确实成功（例如服务端确认解锁、背包功能开放或关卡完成），不要只依据按钮被点击。
2. 若当前 Guide 需要因该操作结束，则调用 `Stop()`；这代表取消/中止，不触发 `OnCompleted`。
3. 检查业务进度是否满足下一套 Guide 的条件、是否已经展示过（展示记录需由游戏自己的存档/进度系统维护）。
4. 条件满足时调用 `StartGuide(nextConfig)`。若旧 Guide 尚在运行，`StartGuide` 会替换它；如果业务要求保留旧 Guide 的正常完成语义，应先用 `Next()` 完成旧流程，再启动新流程。

```csharp
private void OnInventoryActionSucceeded()
{
  // 业务操作已经成功，旧引导不再适用。
  guideModule.Stop();

  if (progress.HasUnlockedEquipment && !progress.HasSeenEquipmentGuide)
  {
    if (!guideModule.StartGuide(equipmentGuideConfig))
      Debug.LogWarning("[InventoryPresenter] Equipment guide config is invalid.");
  }
}
```

上例只展示业务条件触发方式，没有实现已看记录。若要在整套引导完成后保存进度，需要由业务侧在开始前暂存 `GuideId` 并订阅 `guideModule.Runner.OnCompleted`；完成回调不携带 ID，而且此时 `Runner.Config` 已清空。记录何时写入以及如何持久化由游戏自己的进度系统决定。

如果只是业务操作完成后希望当前 Guide 正常进入下一条提示，而不是取消整套 Guide，可在业务确认该操作成功后调用 `Next()`；若每个业务阶段分别使用不同 `GuideId`，则用各自的 GuideConfig，满足阶段条件时启动对应配置。GuideId 的去重、优先级、完成持久化和条件触发目前都需要游戏业务侧自行处理，后续如需统一调度再扩展 Guide 模块。

### 下一阶段：等待业务结果后推进步骤（规划，尚未实现）

当前点击引导目标会立即调用 `Next()`。如果该点击发起异步业务操作（例如抽奖请求），Runner 会先进入下一步；若下一步目标所在 Presenter 尚未打开，Presenter 会等待对应界面打开后再显示目标。这只解决目标界面延迟出现的问题，不代表前一步的业务操作已经成功，也不能表达等待服务端结果的状态。

下一阶段建议为步骤增加明确的推进方式，并区分以下两类步骤：

- **目标点击后推进**：保持现有行为，点击配置目标后立即进入下一步，作为现有配置的默认行为。
- **等待业务确认后推进**：点击目标后隐藏当前步骤的交互并进入等待状态；业务侧在收到成功结果后通知 Guide 进入下一步。若操作失败，则由业务侧通知 Guide 恢复当前步骤，让玩家重试或按业务规则退出。

实现时应遵循以下边界：

- `GuideRunner` 管理等待状态及状态转换；等待不应复用 `Paused`，因为手动暂停与等待业务结果的恢复条件不同。
- `GuideModule` 提供明确的等待完成/失败入口。具体 API 名称待实现时确定；成功通知应能使流程只推进一次，失败通知应能恢复当前步骤。
- 发起抽奖、判断服务端成功或失败等业务规则仍由业务模块负责；Guide 不直接发送协议，也不解析业务响应。
- 业务确认成功并进入下一步后，如果目标 Presenter 尚未打开，继续使用现有 Presenter 打开事件等待机制显示目标。
- `Stop()`、替换引导及 Presenter 关闭时，应清理等待状态和相关订阅，避免迟到的异步回调推进已结束或已替换的引导。

抽奖流程的预期顺序为：点击抽奖目标 → Guide 等待业务确认 → 服务端成功后业务侧通知 Guide 推进 → Guide 等待奖励弹窗打开 → 在奖励弹窗显示下一步。服务端失败时不应自动推进到奖励步骤。

建议验收：等待期间普通点击不能推进步骤；成功通知只推进一次；失败后仍停留在可重试的当前步骤；等待期间停止或替换 Guide 后，迟到的业务回调不影响新流程；现有未配置等待方式的步骤保持原有点击即推进行为。

## 7. 验收检查

- 正确的配置启动后，首步显示且进度为 `1 / 总步数`。
- Running 状态的 Next 推进；Paused 状态的 Next 无效。
- 最后一步再次 Next 后面板隐藏，状态为 `Completed`。
- Stop 后面板隐藏、状态为 `None`，不会触发 `OnCompleted`。
- 空配置和空步骤返回失败，不产生空引用异常。
- 每个步骤恰好配置一种目标方式；路径方式能定位带 UIButton 或 Button 的目标并在点击后推进。
- 连续 Bind 不重复订阅；Unbind 后不再响应 Runner 事件。
- 业务操作仅在成功条件满足后切换 Guide；模块本身不会自动检测业务操作。

静态编译不等同于 Unity Play Mode 验收。GuideDemo 场景应在 Unity Editor 中运行并验证交互。
