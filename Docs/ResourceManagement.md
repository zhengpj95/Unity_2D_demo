# 资源管理系统

## 1. 文档目标

本文记录项目资源管理的当前实现、下一阶段改造顺序和长期演进方向。判断某项能力是否已经落地时，以当前分支代码为准；本文中“后续情况”和“未来情况”均是规划，不代表已经实现。

资源系统的目标是让业务代码只依赖稳定的资源键和统一接口，不直接依赖 `Resources`、Addressables 或 AssetBundle 的具体 API。

## 2. 当前情况

### 2.1 当前结构

核心代码位于：

```text
Assets/Scripts/Framework/Resource/
├── IAssetLoader.cs            # 同步、异步加载契约
├── AssetLoader.cs             # 项目统一访问入口与迁移期回退顺序
├── ResourcesAssetLoader.cs    # 未迁移资源的 Resources 后端
└── AddressablesAssetLoader.cs # 已迁移资源的 Addressables 后端
```

当前调用关系：

```text
UIManager / AudioManager / 后续业务调用方
                    ↓
              AssetLoader
                    ↓
             IAssetLoader
                    ↓
        ResourcesAssetLoader
          ├─ 命中：返回 Resources 资源
          └─ 未命中
                ↓
        AddressablesAssetLoader
          ├─ Addressables.LoadAssetAsync<T>
          └─ 持有成功加载句柄
```

`AssetLoader` 是项目级纯 C# 单例，不需要挂载到 GameObject，也不参与逐帧更新。迁移期间优先查询 `ResourcesAssetLoader`，未命中时再查询 `AddressablesAssetLoader`。具体后端 API 只允许出现在各自实现中；其他 Framework 和业务代码不得新增散落的 `Resources.Load`、`Resources.LoadAsync` 或 Addressables API。当前四个 Survivor UI Prefab 已统一迁入本地 `SurvivorUI` Group。

### 2.2 已实现 API

同步加载：

```csharp
GameObject prefab = AssetLoader.Instance.Load<GameObject>("View/SurvivorHome");
```

异步加载：

```csharp
GameObject prefab = await AssetLoader.Instance.LoadAsync<GameObject>("View/SurvivorHome");
```

接口行为：

- `assetKey` 为空、空白时抛出 `ArgumentException`。
- 找不到资源或资源类型不匹配时返回 `null`。
- 泛型类型必须继承 `UnityEngine.Object`。
- 异步加载必须从 Unity 主线程发起；迁移期间先使用 `Resources.LoadAsync<T>`，未命中时使用 `Addressables.LoadAssetAsync<T>`。
- 当前异步接口不支持取消、进度回调、超时或加载优先级。

### 2.3 已接入调用方

| 调用方                 | 当前方式 | 用途                                |
| ---------------------- | -------- | ----------------------------------- |
| `UIManager.ShowUI`     | 同步     | 加载普通 UI Prefab。                |
| `UIManager.OpenWindow` | 同步     | 加载 Presenter 对应的 View Prefab。 |
| `AudioManager.PlayBGM` | 同步     | 加载背景音乐。                      |
| `AudioManager.PlaySfx` | 同步     | 加载音效。                          |

资源层已经支持异步加载，但现有 UI 和音频调用链尚未迁移为异步。保留同步接口是为了避免在这一阶段将 `BaseModule.OpenWindow`、Presenter 创建和既有按钮回调整体改成异步流程。

### 2.4 当前资源键规则

Resources 后端仍以最近的 `Resources` 目录为根；Addressables 后端使用 Group 中配置的 Address。迁移时保持原资源键不变，键不包含文件扩展名。

例如：

```text
实际文件：Assets/Prefabs/Vampire Survivors-like/Addressables/View/SurvivorHome.prefab
资源键：View/SurvivorHome

实际文件：Assets/Resources/Audio/game-start-6104.mp3
资源键：Audio/game-start-6104
```

项目允许存在多个 `Resources` 目录，但这些目录共享同一个逻辑资源键空间。不得在不同 `Resources` 目录中创建类型相同、资源键相同的文件，否则调用方无法明确指定目标资源。

Presenter 的 `PrefabPath` 当前实际表示“资源键”。该属性名暂时保留以减少无关改动，后续可以在统一迁移时改为 `AssetKey`。

### 2.5 缓存与实例所有权

当前各层职责如下：

- `AssetLoader`：加载资源对象，目前不额外缓存，也不实例化 Prefab。
- `UIManager`：缓存已经实例化的 UI GameObject 和 Presenter。
- `PoolManager`：接收外部提供的 Prefab，缓存并复用实例；它不通过资源键加载 Prefab。
- 场景和 MonoBehaviour：可以通过 Inspector 持有固定资源引用，这类引用不需要经过 `AssetLoader`。

对象池与资源加载保持分离：

```text
场景序列化引用或 AssetLoader 加载 Prefab
                    ↓
             PoolManager.Alloc
                    ↓
             PoolManager.Free
```

当前 Resources 后端没有显式资源句柄。`AddressablesAssetLoader` 会持有成功加载的句柄，并在 `GameMgr.OnDestroy` 中统一释放；这满足当前 Survivor UI 的迁移验证，但还没有实现按单个 UI、音频或对象池生命周期释放。`PoolManager.ClearPool` 只清理池中未使用实例，不能证明该 Prefab 已没有活跃实例。

`PoolManager.Preload(prefab, count)` 中的 `count` 表示该 Prefab 池期望的最少可用缓存数量。重复进入场景时会复用已有缓存，只创建 `count - 当前缓存数` 的缺口，避免预加载在每次场景进入时固定追加对象。

普通 GameObject 池还提供每个 Prefab 独立的缓存策略：默认最多保留 `64` 个隐藏实例，空闲超过 `30` 秒且高于 `Preload` 最低保留量的对象，由 `GameMgr` 驱动的 `PoolManager.OnUpdate` 每 `5` 秒最多销毁 `16` 个。缓存达到上限后，新活跃对象仍可正常创建；多余对象归还时会执行 `OnFree` 后直接销毁。业务需要不同参数时可调用 `ConfigurePool(prefab, minRetained, maxCachedCount, idleLifetime)`。该策略不作用于 UI 池，也不自动回收仍在场上的活跃对象。

### 2.6 当前限制

当前尚未实现：

- 资源加载结果句柄和显式释放 API；
- 相同资源的异步请求合并；
- 加载缓存、引用计数和依赖资源统计；
- 异步取消、超时、进度回调和失败原因分类；
- 远端 Addressables Group、远端 Catalog、下载和内容更新；
- 资源键常量、强类型资源引用或自动生成注册表；
- 编辑器资源键重复检查和构建前校验；
- 资源加载耗时、命中率和内存占用诊断。

## 3. 后续情况

下一阶段应在保持 `AssetLoader` 统一入口的前提下，按以下顺序推进。

### 3.1 完善异步调用链

优先为 UI 增加独立的异步入口，例如 `OpenWindowAsync`，保留现有同步接口用于启动期的小型常驻资源。异步流程需要保证：

1. 同一个界面重复打开时只产生一次加载请求；
2. Presenter 或 Module 已关闭、释放后，完成回调不再创建界面；
3. 加载失败时返回明确结果并恢复按钮或 Loading 状态；
4. Unity 对象的实例化、父子节点设置和界面生命周期仍在主线程执行；
5. 不使用难以追踪异常的 `async void`，Unity 生命周期和按钮入口除外且必须自行捕获异常。

音频是否异步加载应按使用场景区分：短促的即时反馈音效适合提前加载或缓存；体积较大的 BGM 可以异步加载并在完成后淡入。

### 3.2 定义资源所有权与释放

在引入 Addressables 前，应先设计调用方能够遵守的所有权模型。建议加载结果不只返回裸资源，而是返回可释放句柄，句柄至少保存：

- 资源键；
- 实际资源；
- 后端句柄或内部标识；
- 加载状态；
- 释放入口。

UI、音频和对象池需要分别明确释放时机：

| 使用方        | 建议释放时机                               |
| ------------- | ------------------------------------------ |
| 临时窗口      | Presenter 销毁且无缓存需求后。             |
| 常驻 Home/HUD | UIManager 释放或模块明确清理缓存后。       |
| BGM/音效      | 停止播放且音频缓存不再需要后。             |
| 池化 Prefab   | 该 Prefab 的池内实例和活跃实例全部清理后。 |
| 场景资源      | 场景退出且相关异步回调全部失效后。         |

### 3.3 补齐对象池协作

若 Prefab 由可释放后端加载，对象池需要按资源或 Prefab 统计活跃实例。只有满足以下条件时才允许资源层释放原始 Prefab：

```text
池内闲置实例数量 = 0
并且
活跃实例数量 = 0
并且
没有进行中的预加载或创建请求
```

对象池仍不负责决定资源来自 Resources 还是 Addressables；它只需要提供池清理完成状态，让上层资源所有者安全释放句柄。

### 3.4 加入诊断和校验

建议逐步加入：

- 加载失败日志包含资源键、期望类型、后端名称和调用模块；
- 开发环境检测同一资源键对应多个资源；
- 统计同步加载发生的位置，避免在战斗高频路径同步加载；
- 统计加载耗时、缓存命中和未释放句柄；
- 构建前校验 Presenter 的资源键是否可解析。

## 4. 未来情况

### 4.1 技术选型：Addressables 与 YooAsset

项目当前使用 Unity 2022.3.62f2c1。`com.unity.addressables` 1.21.21 已安装，`AddressablesAssetLoader` 和本地 Catalog 已落地，四个 Survivor UI Prefab 已迁入本地 `SurvivorUI` Group；其余资源仍通过 `ResourcesAssetLoader` 加载。YooAsset 尚未引入。Addressables 1.21.21 与 YooAsset 3.x 均支持 Unity 2022.3，因此版本兼容性不是本项目的决策因素。

| 维度         | Addressables                                                                         | YooAsset                                                                                     |
| ------------ | ------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------- |
| 维护与生态   | Unity 官方包，和 Unity 编辑器、AssetReference、Content Catalog、Profiler 工作流一致  | 第三方开源方案，提供独立的编辑器配置、构建、运行时与调试工作流                               |
| 基础资源能力 | 地址、Label、Group、本地与远端内容、依赖管理、异步加载与引用计数释放                 | 资源包、地址、标签、同步/异步加载、引用计数释放，支持原生文件包                              |
| 内容更新     | 通过远端 Catalog 与 Content Update Build 更新，发布时需要管理 Catalog 和 Bundle 上传 | 运行时资源版本、清单、下载器与补丁部署是核心能力，提供断点续传、校验、重试及版本回退等工作流 |
| 项目接入成本 | 与现有 `AssetLoader` 的异步加载和句柄释放模型接近；后续可逐步从 Resources 迁移       | 功能覆盖更广，但需要接受 Package、文件系统、初始化模式、构建和部署规范这一整套流程           |
| 更适合的场景 | 中小型项目渐进迁移；需要 Unity 官方维护、标准本地/远端资源加载和常规 DLC/热更新      | 明确需要多资源包、边玩边下载、复杂 CDN 更新、版本回退、原生文件、MOD 或小游戏平台资源管线    |

**当前决策：后续资源后端优先选择 Addressables。**

原因是本项目目前资源量和业务形态仍处于小型单工程阶段，当前需求是把 `Resources` 调用收敛到 `AssetLoader` 并补齐异步、所有权和对象池协作；仓库中没有已经落地的 CDN、热更新、分包、原生文件或多 Package 需求。Addressables 能在不改变调用方资源键的前提下完成渐进迁移，并由 Unity 官方包维护。

安装 Addressables 和迁移当前 Survivor UI 不等于已完成资源系统迁移。后续仍需完成第 3 章中的异步 UI 调用链、按资源释放语义、并发请求合并和对象池活跃实例统计，并保持 `IAssetLoader` 作为业务层唯一入口。

出现以下任一明确需求时，重新评估并可改选 YooAsset：

1. 需要多个独立资源 Package，或不同业务/内容需要分工程构建；
2. 需要边玩边下载、下载队列、断点续传、校验重试、版本回退等完整运行时更新管线；
3. 需要管理 Unity 原生文件、MOD 内容或面向微信/抖音小游戏的资源交付；
4. 团队愿意维护 YooAsset 的初始化、构建、部署与 CDN 版本规范，并将该工作流纳入发布流程。

选型依据：Unity Addressables 文档说明其支持本地/远端资源、Group、Catalog 和引用计数释放；YooAsset 官方文档说明其支持 Unity 2022.3、Package、运行模式、补丁部署与资源下载流程。

### 4.2 推荐落地形态：Addressables 后端

后续将 UI、敌人、武器、场景和音频逐步迁移到 Addressables 后端；资源热更新、远端下载和包体拆分需求出现后，再配置对应的 Group、Catalog 与发布流程。目标调用关系为：

```text
业务层 / Framework
       ↓
  AssetLoader
       ↓
  IAssetLoader
       ↓
AddressablesAssetLoader
  ├─ 本地 Group
  ├─ 远端 Group
  ├─ Content Catalog
  └─ 引用计数与 Release
```

迁移后业务代码继续使用资源键或强类型引用，不直接出现 `Addressables.LoadAssetAsync`。Addressables 的 `AsyncOperationHandle` 由资源后端或统一资源句柄持有，不能泄漏到 Presenter、Proxy 或具体玩法代码中。

### 4.3 资源组织

未来可以按“同时加载、同时释放”的生命周期组织资源组，而不是单纯按文件类型拆分：

```text
Bootstrap       # 启动和 Loading 必需资源
SharedUI        # 多模块共用 UI、字体和图集
SurvivorHome    # Survivor 局外界面
SurvivorBattle  # 战斗角色、敌人、武器、特效和 HUD
AudioCommon     # 通用点击与提示音
AudioMusic      # 可独立下载或切换的音乐
```

共享资源需要单独分组，避免它被多个业务包重复打包。资源地址应稳定，不直接依赖容易变化的物理目录。

### 4.4 内容更新与容错

如果未来启用远端内容，应补充：

- Catalog 版本检查与更新策略；
- 下载大小确认、进度展示和取消；
- 断网重试、超时、磁盘空间不足和校验失败处理；
- CDN 地址按开发、测试、正式环境切换；
- 上一版本资源与 Catalog 的回滚能力；
- 核心启动资源本地兜底，避免远端不可用时无法进入游戏。

### 4.5 测试能力

`IAssetLoader` 应支持在 Edit Mode 或业务测试中替换为测试实现，用于验证：

- 资源不存在；
- 异步延迟完成；
- 重复请求；
- 页面关闭后加载才完成；
- 加载异常；
- 资源释放时仍存在池化活跃实例。

测试实现不应依赖真实 Resources 或 Addressables 构建产物。

## 5. 开发规则

新增或修改资源加载代码时遵守以下规则：

1. Framework 和业务层统一通过 `AssetLoader`，不直接调用具体资源后端。
2. Inspector 中明确且固定的场景引用可以继续序列化，不必为了形式统一改成运行时加载。
3. `Update`、武器攻击、敌人生成等高频路径禁止临时同步加载资源。
4. Prefab 的加载、实例化和对象池复用是三个不同职责，不合并到同一个 Manager。
5. 异步完成后必须重新检查调用方生命周期，不能假设请求期间界面或场景仍然存在。
6. 引入释放机制后，加载和释放必须成对，并先清理所有依赖该资源的实例。
7. 新增资源键时避免跨 `Resources` 目录重名；未来迁移 Addressables 后保持地址稳定。
8. 修改资源层公共契约、后端、生命周期或跨模块使用方式时，同步本文和 `Docs/Architecture.md`。

## 6. 分阶段验收

### 当前阶段

- 具体 Resources 与 Addressables API 只出现在各自后端中。
- `SurvivorHome`、`SurvivorMain`、`SurvivorGameOver` 和 `SurvivorSkillSelectPanel` 通过本地 `SurvivorUI` Group 加载，其余 UI 和音频仍通过 Resources 加载。
- `LoadAsync<T>` 能在主线程异步返回资源或 `null`。
- 空资源键能得到一致的参数异常。

### 下一阶段

- 至少一条正式 UI 流程使用异步加载并正确处理重复请求、关闭和失败。
- 相同资源的并发请求能够合并。
- 资源句柄能够定位未释放资源。
- 对象池能报告指定 Prefab 是否仍有活跃实例。

### Addressables 阶段

- 业务代码不直接依赖 Addressables API 或 `AsyncOperationHandle`。
- 本地和远端 Group 均能通过同一资源键加载。
- 场景切换、UI 关闭、音频停止和对象池清理后引用计数正确归零。
- 断网、下载失败、Catalog 更新失败时有明确恢复路径。

## 7. 选型资料

- Unity Addressables：<https://docs.unity.cn/Manual/com.unity.addressables>、<https://docs.unity.cn/Packages/com.unity.addressables@1.21/manual/AddressableAssetsOverview.html>、<https://docs.unity.cn/Packages/com.unity.addressables@1.21/manual/MemoryManagement.html>
- YooAsset：<https://www.yooasset.com/docs/guide-editor/QuickStart>、<https://www.yooasset.com/docs/guide-editor/AssetBundleCollector>、<https://www.yooasset.com/docs/guide-editor/AssetBundleDeployer>、<https://github.com/tuyoogame/YooAsset>
