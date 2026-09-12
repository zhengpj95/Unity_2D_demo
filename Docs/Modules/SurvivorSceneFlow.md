# Survivor 主界面与战斗场景流程

## 1. 目标

将当前“Launcher 登录后直接进入战斗”的流程调整为：

```text
Launcher.unity
  → 登录成功
  → SurvivorHomePresenter
  → 点击 btnStart
  → SurvivorsDemo.unity
  → 战斗 / 升级 / GameOver
  → 返回主界面
  → Launcher.unity + SurvivorHomePresenter
```

`SurvivorHome` 属于局外主界面，不属于战斗 HUD。它在 `Launcher.unity` 激活期间打开；Home 显示时不加载 `SurvivorsDemo.unity`，因此不会创建 Player、EnemyDirector、WeaponManager 或 Wave 运行时对象，也不依赖 `Time.timeScale = 0` 阻止战斗。

## 2. 场景与持久对象职责

| 对象 | 生命周期 | 职责 |
| --- | --- | --- |
| `Launcher.unity` | 登录或返回主界面时加载 | 承载登录入口与 Home 所需的非战斗场景环境。 |
| `SurvivorsDemo.unity` | 点击开始战斗后加载 | 承载 Player、EnemyDirector、WeaponManager、DropItemManager、Wave 和地图。 |
| `GameMgr` | `DontDestroyOnLoad` | 初始化并持续驱动 Module、Network、Timer 和 Pool。 |
| `UILauncher/UIRoot` | `DontDestroyOnLoad` | 持有 UIManager 的 Main/Window/Model/Tip 层和已缓存 Presenter 的 View。 |
| `SurvivorModule` | 随 GameMgr 常驻 | 注册 Home、战斗 HUD、升级选择和 GameOver Presenter，向流程控制器提供 UI 开关入口。 |
| `SurvivorGameplayController` | 随 SurvivorModule 常驻 | 编排开始战斗、重开、返回主界面及场景切换前后的状态收口。 |

返回 `Launcher.unity` 时，新场景中的重复 `GameMgr` 和 `UILauncher` 会按现有单例规则销毁；原有持久化 UIRoot、UIManager 和 Presenter 缓存继续使用。

## 3. 界面职责

### SurvivorHomePresenter

- `PrefabPath` 为 `Prefabs/SurvivorHome`，显示在 `UILayerIndex.Main`。
- `Launcher/UIRoot/UIMain` 的独立 Canvas 必须同时挂载启用的 `GraphicRaycaster`；根 UIRoot 的射线检测不会代替子 Canvas 检测 Home 按钮。缺少该组件时界面可见，但开始按钮无法收到点击。
- 接收类型明确的开始战斗回调。
- `btnStart` 只提交“开始战斗”请求，不直接调用 `SceneManager`、不直接修改 `Time.timeScale`。
- 加载战斗期间保持显示，场景激活并打开战斗 HUD 后由 `SurvivorModule` 隐藏，避免加载空白；返回 Launcher 后从 UIManager 缓存重新显示。

### SurvivorMainPresenter

- 继续作为 `SurvivorsDemo` 的局内 HUD，不与 SurvivorHome 合并。
- 战斗场景加载完成后显示；返回 Home 前隐藏。

### SurvivorGameOverPresenter

- “再来一局”继续重载 `SurvivorsDemo.unity`。
- 原 `btnQuit` 调整为“返回主界面”：Presenter 关闭自身后通过回调交给 `SurvivorGameplayController`。
- Presenter 不直接清理敌人、对象池、场景或局内 Model。

## 4. 状态与数据规则

`SurvivorModel` 继续只保存一局战斗数据，不增加 Home UI 状态。Home 阶段没有加载战斗场景，因此无需给敌人、武器和玩家分别增加“Home 暂停”判断。

开始新战斗前调用 `SurvivorProxy.ResetRound()`，保证生命、等级、经验、击杀、局内金币、武器等级与本局玩家属性从初始状态开始。当前金币仍属于本局数据；未来实现局外货币时应放入独立持久数据，而不是依赖本局 Model。

场景切换期间使用独立的“正在切换”标记防止按钮重复点击。进入战斗完成后恢复 `Time.timeScale = 1`；GameOver 返回 Home 时，在旧战斗场景卸载前先回收活跃敌人、掉落物和武器攻击对象。

## 5. 完整调用流程

### 登录进入 Home

```text
LoadingBehaviour.OnLogin
  → btnLogin 使用 UIButton.Clicked，通过 OnEnable/OnDisable 代码绑定与解绑；点击后隐藏按钮、显示进度条
  → UIProgressBar 使用非缩放时间从 0.00% 播放到 100.00%，持续 1 秒
  → SurvivorModule.OpenSurvivorHome
  → 隐藏 Launcher/Loading 登录节点
```

登录进度属于展示过程，不代表真实网络或资源加载进度。重复点击不会重启计时；Loading 节点禁用时取消协程。`LoadingBehaviour.progressBar` 优先使用 Inspector 绑定，未绑定时查找子节点中的 `UIProgressBar`；缺失时输出错误并保留登录界面。

### Home 开始战斗


```text
SurvivorHomePresenter.btnStart
  → SurvivorHomeArgs.OnStartBattle
  → SurvivorGameplayController.StartBattle
  → 防重复切换
  → SurvivorProxy.ResetRound
  → 保持 Home 显示，覆盖加载期间的画面
  → SceneManager.LoadSceneAsync("SurvivorsDemo")
  → Time.timeScale = 1
  → SurvivorModule.OpenSurvivorMain
  → SurvivorModule.HideSurvivorHome
```

### GameOver 返回 Home

```text
SurvivorGameOverPresenter.btnQuit
  → 关闭 GameOver
  → SurvivorGameOverArgs.OnReturnHome
  → SurvivorGameplayController.ReturnToHome
  → 关闭升级选择 / 隐藏局内 HUD
  → 回收 Enemy、DropItem、WeaponEffect
  → SceneManager.LoadSceneAsync("Launcher")
  → Time.timeScale = 1
  → SurvivorProxy.ResetRound
  → SurvivorModule.OpenSurvivorHome
```

### GameOver 再来一局

保持当前已验收逻辑：关闭 GameOver，回收本局活跃对象，重置本局 Model，并重载当前 `SurvivorsDemo.unity`。

## 6. 异常与兼容规则

- 找不到 SurvivorModule 或 Home Prefab 时保留登录界面，并输出包含模块/资源名的错误日志。
- 场景加载请求返回空操作时恢复当前界面与按钮状态，不让流程永久停在“正在切换”。
- 完整 UI 与 Model 流程以 `Launcher.unity` 为正式启动场景；直接从 `SurvivorsDemo.unity` 进入 Play Mode 不会自动补建 Launcher 中的常驻 GameMgr/UIRoot，不列入本阶段正式流程。
- Home 和 Main 使用不同的 `SurvivorViewType`，避免 Presenter 缓存键冲突。
- 隐藏 Presenter 使用 `UIManager.HidePresenter` 保留缓存；模块释放时仍由 BaseModule 统一销毁。
- 本阶段不新增全局 SceneManager、Service 或第二套事件系统。

## 7. 验收清单

1. 从 `Launcher.unity` 点击登录后仍停留在 Launcher，登录内容隐藏，SurvivorHome 显示。
2. Home 显示期间场景中不存在 Survivor 战斗对象，也不会推进 Wave 或生成敌人。
3. 连续点击 `btnStart` 只发起一次场景加载。
4. `SurvivorsDemo.unity` 加载完成后 Home 隐藏、局内 HUD 显示、时间正常推进。
5. GameOver 点击“再来一局”仍能正常重开战斗。
6. GameOver 点击“返回主界面”会回收活跃对象、加载 Launcher，并显示 Home 而不是登录内容。
7. 再次点击 Home 的开始按钮能够进入一局全新战斗，生命、经验、等级、击杀及局内金币均已重置。

## 8. 文档同步规则

后续修改 Home、登录入口、战斗场景名、GameOver 返回逻辑或跨场景 UI 生命周期时，必须同步本文与 `Docs/Modules/Survivor.md`；若修改 `GameMgr`、`UILauncher`、UIManager 或 Module 生命周期，还必须同步 `Docs/Architecture.md`。
