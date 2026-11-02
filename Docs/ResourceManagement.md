# 资源管理系统

## 1. 文档目标

本文记录项目资源管理的当前实现、剩余工作和长期演进方向。判断某项能力是否已经落地时，以当前分支代码为准；“后续工作”和“未来情况”均是规划，不代表已经实现。

资源系统的目标是让业务代码只依赖稳定的资源键和统一接口，不直接依赖 `Resources`、Addressables 或 AssetBundle 的具体 API。

## 2. 当前情况

### 2.1 当前结构

核心代码位于：

```text
Assets/Scripts/Framework/Resource/
├── README.md                  # 类型职责与调用方式
├── IAssetLoader.cs            # 同步、异步加载契约
├── AssetLoader.cs             # 项目统一访问入口与迁移期回退顺序
├── AssetHandle.cs             # 资源租约句柄与后端加载结果
├── ResourcesAssetLoader.cs    # 未迁移资源的 Resources 后端
└── AddressablesAssetLoader.cs # 已迁移资源的 Addressables 后端
```

Resource 目录内各类型的职责、示例和句柄释放规则详见 [Resource README](../Assets/Scripts/Framework/Resource/README.md)。

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

`AssetLoader` 是项目级纯 C# 单例，不需要挂载到 GameObject，也不参与逐帧更新。迁移期间优先查询 `ResourcesAssetLoader`，未命中时再查询 `AddressablesAssetLoader`。具体后端 API 只允许出现在各自实现中；其他 Framework 和业务代码不得新增散落的 `Resources.Load`、`Resources.LoadAsync` 或 Addressables API。当前四个 Survivor UI Prefab 已统一迁入本地 `SurvivorUI` Group；跨模块通用 UI 使用本地 `CommonView` Group；公共音频 Clip 使用本地 `Audio` Group。

### 2.2 已实现 API

同步加载的兼容入口会把资源持有到 `AssetLoader.ReleaseAll()`；新代码应优先使用显式句柄：

```csharp
using AssetHandle<GameObject> prefabHandle =
  AssetLoader.Instance.LoadHandle<GameObject>("Prefabs/Survivor/View/SurvivorHome");
GameObject prefab = prefabHandle?.Asset;
```

异步加载使用同样的显式所有权。不同调用方通过 `Retain()` 为各自的生命周期增加独立引用：

```csharp
AssetHandle<GameObject> prefabHandle =
  await AssetLoader.Instance.LoadHandleAsync<GameObject>("Prefabs/Survivor/View/SurvivorHome");
try
{
  GameObject prefab = prefabHandle?.Asset;
  // 在句柄存活期间实例化或使用资源。
}
finally
{
  prefabHandle?.Dispose();
}
```

接口行为：

- `assetKey` 为空、空白时抛出 `ArgumentException`。
- 找不到资源或资源类型不匹配时返回 `null`。
- 泛型类型必须继承 `UnityEngine.Object`。
- 异步加载必须从 Unity 主线程发起；迁移期间先使用 `Resources.LoadAsync<T>`，未命中时使用 `Addressables.LoadAssetAsync<T>`。
- 同一资源键和类型的并发异步请求共用一个后端加载；请求完成后只保留各调用方的句柄引用，不做跨调用的常驻资源缓存。
- `AssetHandle<T>.Dispose()` 可重复调用；最后一个句柄释放时执行后端释放回调。Addressables 后端调用 `Addressables.Release`；Resources 后端无单资源句柄，释放租约后由 Unity 按对象可达性管理内存。
- 旧 `Load<T>` / `LoadAsync<T>` 为兼容保留，返回裸资源并持有句柄到全局 `ReleaseAll()`；新代码不得用它们替代按生命周期释放的句柄 API。
- 当前没有加载取消、超时、进度回调、优先级或加载失败分类。

### 2.3 已接入调用方

| 调用方 | 当前方式 | 句柄释放时机 |
| --- | --- | --- |
| `BaseModule.OpenWindow` | 统一 Task 接口；内部处理缓存或首次异步加载 | Presenter 销毁；隐藏缓存时保留 |
| 旧式 `UIManager.ShowUI` | 同步兼容接口 | UI 销毁时释放；隐藏缓存时保留 |
| `BaseModule.OpenWindowIn` | Task 接口；用于局部容器挂载 | Presenter 销毁；取消或模块释放时不创建 UI 并释放迟到句柄 |
| Launcher 登录后的 Survivor Home | 统一 Presenter 打开接口 | Presenter 销毁；登录宿主关闭时取消待打开请求 |
| `AudioManager.PlayBGMAsync` / `PlaySfxAsync` | 异步句柄 | BGM 停止/替换；SFX 播放结束/停止 |
| `ListTab` 图标 | 同步句柄 | ListTab 宿主销毁 |
| `PoolManager.PreloadAsync` | 异步句柄 | 清池且所有活跃实例归还或被检测销毁 |

界面打开对调用方只提供一个 Task 接口；无论界面命中缓存还是需要首次加载，调用方都使用同一方法并等待结果。底层资源加载仍有同步兼容 API，但不影响 Presenter 界面的打开方式。

### 2.4 当前资源键规则

Resources 后端仍以最近的 `Resources` 目录为根；Addressables 后端使用 Group 中配置的 Address。迁移时保持原资源键不变，键不包含文件扩展名。

例如：

```text
实际文件：Assets/Prefabs/Survivor/View/SurvivorHome.prefab
资源键：Prefabs/Survivor/View/SurvivorHome

实际文件：Assets/Prefabs/CommonView/AlertTipsPanel.prefab
资源键：Prefabs/AlertTipsPanel

实际文件：Assets/Arts/Audio/game-start-6104.mp3
资源键：Audio/game-start-6104
```

项目允许存在多个 `Resources` 目录，但这些目录共享同一个逻辑资源键空间。不得在不同 `Resources` 目录中创建类型相同、资源键相同的文件，否则调用方无法明确指定目标资源。

Presenter 的 `PrefabPath` 当前实际表示“资源键”。该属性名暂时保留以减少无关改动，后续可以在统一迁移时改为 `AssetKey`。

### 2.5 缓存与实例所有权

当前各层职责如下：

- `AssetLoader`：协调后端加载、合并同键同类型并发异步请求，并通过句柄引用计数释放底层资源；不实例化 Prefab，不缓存已完成的资源。
- `UIManager`：缓存已经实例化的 UI GameObject、Presenter 及其 Prefab 句柄；隐藏时保留，销毁时释放。
- `PoolManager`：复用实例；通过 `Preload(AssetHandle<GameObject>, count)` 或 `PreloadAsync(assetKey, count)` 时独立保留 Prefab 句柄。
- `AudioManager`、`ListTab`：分别按音频播放周期、宿主组件周期持有资源句柄。
- 场景和 MonoBehaviour：可以通过 Inspector 持有固定资源引用，这类引用不需要经过 `AssetLoader`。

对象池与资源加载保持分离：

```text
场景序列化引用或 AssetLoader 加载 Prefab
                    ↓
             PoolManager.Alloc
                    ↓
             PoolManager.Free
```

`AssetHandle<T>` 是调用方的所有权令牌，不暴露 Addressables 操作句柄。Addressables 后端只在所有租约释放后调用 `Addressables.Release`；Resources 后端没有单项释放 API，租约释放表示调用方已不再持有，不会强行卸载仍被 Unity 对象引用的资源。`GameMgr.OnDestroy` 先释放 UI 和普通池的缓存，再用 `AssetLoader.ReleaseAll()` 兜底清理未正确归还的租约。

池化资源句柄只在池不再接受对象后释放：`ClearPool` / `ClearAll` 销毁池内缓存并将池标记为退役；仍在场的活跃对象归还时会销毁，活跃对象被外部销毁时由定期清理发现，最后一个活跃实例消失后释放 Prefab 句柄。原有 `Preload(GameObject, count)` 不取得资源所有权，只有句柄重载或 `PreloadAsync` 能建立此关系。

`PoolManager.Preload(prefab, count)` 中的 `count` 表示该 Prefab 池期望的最少可用缓存数量。重复进入场景时会复用已有缓存，只创建 `count - 当前缓存数` 的缺口，避免预加载在每次场景进入时固定追加对象。

普通 GameObject 池还提供每个 Prefab 独立的缓存策略：默认最多保留 `64` 个隐藏实例，空闲超过 `30` 秒且高于 `Preload` 最低保留量的对象，由 `GameMgr` 驱动的 `PoolManager.OnUpdate` 每 `5` 秒最多销毁 `16` 个。缓存达到上限后，新活跃对象仍可正常创建；多余对象归还时会执行 `OnFree` 后直接销毁。业务需要不同参数时可调用 `ConfigurePool(prefab, minRetained, maxCachedCount, idleLifetime)`。该策略不作用于 UI 池，也不自动回收仍在场上的活跃对象。

### 2.6 当前限制

当前尚未实现：

- 已完成资源的跨调用缓存、依赖资源统计和详细租约诊断；
- 后端加载过程的取消、超时、进度回调、优先级和失败原因分类；
- 远端 Addressables Group、远端 Catalog、下载和内容更新；
- 资源键常量、强类型资源引用或自动生成注册表；
- 编辑器资源键重复检查和构建前校验；
- 资源加载耗时、命中率和内存占用诊断。

## 3. 当前实现边界与后续事项

保持 `AssetLoader` 为唯一入口；剩余能力按实际产品需求推进。

### 3.1 异步调用与生命周期

当前 `BaseModule.OpenWindow` 与 `OpenWindowIn` 统一返回 Task；UIManager 内部直接返回缓存 Presenter 的已完成 Task，或异步加载 Prefab 后创建界面。同一 ViewKey 的并发请求共享一个 UI 创建任务；Module 释放或 `CancelPendingOpen` 会阻止迟到加载创建界面，并在资源完成后释放句柄。后端加载本身不取消，调用方仍需处理 Task 异常和资源缺失结果。

`LoadingBehaviour` 使用统一打开流程打开 Survivor Home，并在宿主关闭时取消待打开请求。所有 Presenter 调用方均使用同一 Task API，不需要根据缓存命中或首次加载选择不同方法。

### 3.2 对象池和资源释放

带资源句柄的池满足以下条件后才释放 Prefab：
```text
池内闲置实例数量 = 0
并且
活跃实例数量 = 0
```

对象池不判断资源来自 Resources 还是 Addressables。`PreloadAsync` 会持有独立句柄；`ClearAll` 会使尚未完成的预热请求失效，防止请求在全局清理后重建池。清池会销毁闲置实例，活跃实例归还时销毁并更新计数。外部直接销毁的活跃对象会在池更新扫描中清理记录。原有基于 Inspector Prefab 的池调用维持原行为，但不代表 AssetLoader 所有权。

### 3.3 后续诊断和校验

后续可逐步加入：

- 加载失败日志包含资源键、期望类型、后端名称和调用模块；
- 开发环境检测同一资源键对应多个资源；
- 统计同步加载发生的位置，避免在战斗高频路径同步加载；
- 统计加载耗时、缓存命中和未释放句柄；
- 构建前校验 Presenter 的资源键是否可解析。

## 4. 未来情况

### 4.1 技术选型：Addressables 与 YooAsset

项目当前使用 Unity 2022.3.62f2c1。`com.unity.addressables` 1.21.21 已安装，`AddressablesAssetLoader` 和本地 Catalog 已落地，四个 Survivor UI Prefab 已迁入本地 `SurvivorUI` Group，通用弹窗已迁入本地 `CommonView` Group，公共音频 Clip 已迁入本地 `Audio` Group；其余资源仍通过 `ResourcesAssetLoader` 加载。YooAsset 尚未引入。Addressables 1.21.21 与 YooAsset 3.x 均支持 Unity 2022.3，因此版本兼容性不是本项目的决策因素。

| 维度         | Addressables                                                                         | YooAsset                                                                                     |
| ------------ | ------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------- |
| 维护与生态   | Unity 官方包，和 Unity 编辑器、AssetReference、Content Catalog、Profiler 工作流一致  | 第三方开源方案，提供独立的编辑器配置、构建、运行时与调试工作流                               |
| 基础资源能力 | 地址、Label、Group、本地与远端内容、依赖管理、异步加载与引用计数释放                 | 资源包、地址、标签、同步/异步加载、引用计数释放，支持原生文件包                              |
| 内容更新     | 通过远端 Catalog 与 Content Update Build 更新，发布时需要管理 Catalog 和 Bundle 上传 | 运行时资源版本、清单、下载器与补丁部署是核心能力，提供断点续传、校验、重试及版本回退等工作流 |
| 项目接入成本 | 与现有 `AssetLoader` 的异步加载和句柄释放模型接近；后续可逐步从 Resources 迁移       | 功能覆盖更广，但需要接受 Package、文件系统、初始化模式、构建和部署规范这一整套流程           |
| 更适合的场景 | 中小型项目渐进迁移；需要 Unity 官方维护、标准本地/远端资源加载和常规 DLC/热更新      | 明确需要多资源包、边玩边下载、复杂 CDN 更新、版本回退、原生文件、MOD 或小游戏平台资源管线    |

**当前决策：后续资源后端优先选择 Addressables。**

原因是本项目目前资源量和业务形态仍处于小型单工程阶段，当前需求是把 `Resources` 调用收敛到 `AssetLoader` 并逐步迁移资源；仓库中没有已经落地的 CDN、热更新、分包、原生文件或多 Package 需求。Addressables 能在不改变调用方资源键的前提下完成渐进迁移，并由 Unity 官方包维护。

安装 Addressables 和迁移当前 Survivor UI 不等于已完成资源系统迁移。剩余工作包括远端内容管线、异步取消/进度、租约诊断和构建前校验；业务层继续只依赖 `AssetLoader`。

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
Audio           # 通用点击与提示音
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
- `SurvivorHome`、`SurvivorMain`、`SurvivorGameOver` 和 `SurvivorSkillSelectPanel` 通过本地 `SurvivorUI` Group 加载；`AlertTipsPanel` 通过本地 `CommonView` Group 加载；公共音频 Clip 通过本地 `Audio` Group 加载，其余未迁移资源仍通过 Resources 加载。
- `LoadHandleAsync<T>` 能在主线程异步返回句柄或 `null`，相同资源键和类型的并发请求共享一次加载。
- UI、Audio、ListTab 图标和通过资源句柄预热的对象池按各自生命周期释放资源。
- Launcher 登录后的 Home 通过异步 Presenter 打开；模块关闭、宿主关闭或资源加载失败时不会创建悬空 UI。
- `Load<T>` / `LoadAsync<T>` 兼容入口仍可用，但只在 `ReleaseAll()` 时释放其资源。
- 空资源键能得到一致的参数异常。

### 后续工作

- 增加异步加载取消、进度、优先级及更细的失败原因分类。
- 按实际需要迁移其他同步 UI 流程；保留小型启动期资源的同步接口。
- 增加租约泄漏诊断、加载耗时/命中统计、资源键常量和构建前校验。

### Addressables 阶段

- 业务代码不直接依赖 Addressables API 或 `AsyncOperationHandle`。
- 本地和远端 Group 均能通过同一资源键加载。
- 场景切换、UI 关闭、音频停止和对象池清理后引用计数正确归零。
- 断网、下载失败、Catalog 更新失败时有明确恢复路径。

## 7. 选型资料

- Unity Addressables：<https://docs.unity.cn/Manual/com.unity.addressables>、<https://docs.unity.cn/Packages/com.unity.addressables@1.21/manual/AddressableAssetsOverview.html>、<https://docs.unity.cn/Packages/com.unity.addressables@1.21/manual/MemoryManagement.html>
- YooAsset：<https://www.yooasset.com/docs/guide-editor/QuickStart>、<https://www.yooasset.com/docs/guide-editor/AssetBundleCollector>、<https://www.yooasset.com/docs/guide-editor/AssetBundleDeployer>、<https://github.com/tuyoogame/YooAsset>
