# Resource 资源加载框架

本文介绍 `Assets/Scripts/Framework/Resource/` 中各类型的职责、调用方式和资源释放规则。资源键、后端优先级及当前迁移状态见 [资源管理系统](../../../../Docs/ResourceManagement.md)。

## 目录结构

| 文件 | 类型 | 职责 |
| --- | --- | --- |
| `AssetLoader.cs` | `AssetLoader` | 项目统一资源入口；选择后端、异步请求合并、创建和追踪资源租约。 |
| `AssetHandle.cs` | `AssetHandle<T>`、`AssetLoadResult<T>`、内部租约类型 | 提供显式资源所有权句柄；承载后端加载结果和最后一个引用的释放回调。 |
| `IAssetLoader.cs` | `IAssetLoader` | 资源后端的同步、异步加载契约。 |
| `ResourcesAssetLoader.cs` | `ResourcesAssetLoader` | 使用 Unity `Resources` 加载资源；当前没有单资源释放回调。 |
| `AddressablesAssetLoader.cs` | `AddressablesAssetLoader` | 使用 Addressables 加载资源，并在租约释放时释放 Addressables 操作句柄。 |

业务代码应通过 `AssetLoader` 使用资源，不应自行创建或调用具体后端。

## 先理解“句柄”

这里的“句柄”可以理解成**资源的借用凭证**，它不是资源本身：

- `Asset` 是加载出来的资源本体，例如 Prefab、图片或音频。
- `AssetHandle<T>` 是“我还在使用这份资源”的凭证。
- `Dispose()` 是归还自己的凭证，表示当前使用方不再需要这份资源。
- `Retain()` 是再创建一张独立凭证，让另一个使用方也能持有这份资源。

例如从资源库借一份 Prefab：

```csharp
AssetHandle<GameObject> handle =
  await AssetLoader.Instance.LoadHandleAsync<GameObject>(assetKey);

if (handle == null)
  return; // 没找到资源

try
{
  GameObject prefab = handle.Asset; // 通过凭证取得真正的 Prefab
  // 在这里使用 prefab。
}
finally
{
  handle.Dispose(); // 用完后归还自己的凭证
}
```

哪个对象还要使用资源，哪个对象就应持有自己的句柄；不用时释放。多个使用方各持一份句柄时，一个使用方调用 `Dispose()` 不会影响其他仍持有句柄的使用方。最后一份句柄释放后，Addressables 资源才会收到释放操作；Resources 后端没有单项卸载操作，因此释放句柄表示调用方放弃持有，不保证资源立即从内存卸载。

## 类型职责

### `AssetLoader`

`AssetLoader` 是纯 C# 单例，不需要挂载到 GameObject。它先通过 `ResourcesAssetLoader` 查找资源；未命中时再尝试 `AddressablesAssetLoader`。同一资源键和类型的**并发异步请求**共享一次后端加载，但加载完成后不会作为常驻缓存。

主要 API：

| API | 用途 | 所有权 |
| --- | --- | --- |
| `LoadHandle<T>(assetKey)` | 同步加载并取得句柄。 | 调用方持有并负责释放。 |
| `LoadHandleAsync<T>(assetKey)` | 异步加载并取得句柄；必须从 Unity 主线程发起。 | 调用方持有并负责释放。 |
| `Load<T>(assetKey)` | 兼容旧代码的同步接口，直接返回资源对象。 | 句柄由 `AssetLoader` 持有至 `ReleaseAll()`。新代码优先使用句柄 API。 |
| `LoadAsync<T>(assetKey)` | 兼容旧代码的异步接口，直接返回资源对象。 | 句柄由 `AssetLoader` 持有至 `ReleaseAll()`。 |
| `ReleaseAll()` | 全局生命周期末尾的兜底释放。 | 释放所有仍登记的租约并使未完成的异步加载结果失效。 |

`T` 必须是 `UnityEngine.Object` 类型。空或空白资源键会抛出 `ArgumentException`；资源未找到或类型不匹配时返回 `null`。异步加载过程中的异常会沿 Task 传播，调用方应按自己的界面或游戏流程处理。

### `AssetHandle<T>`

`AssetHandle<T>` 是调用方的资源所有权令牌，不暴露 Addressables 的 `AsyncOperationHandle`。

- `Asset`：获取资源。句柄释放或资源租约已被全局释放后访问会抛出 `ObjectDisposedException`。
- `Retain()`：为另一项独立生命周期创建一个需要单独释放的新句柄。
- `Dispose()`：释放当前句柄；可重复调用。
- `IsReleased`：检查句柄是否已释放。

同一加载结果的租约按引用计数管理。所有句柄释放后，Addressables 后端的释放回调才会执行。Resources 后端没有单项释放 API，释放句柄表示调用方放弃所有权，不会强制卸载仍被引用的 Unity 资源。

`AssetLoadResult<T>` 是后端向 `AssetLoader` 返回的资源与释放回调封装。它用于后端协作，不是业务调用方应直接创建或依赖的 API。`AssetLease<T>` 和 `IAssetLease` 是内部实现类型，负责引用计数与全局兜底登记。

### `IAssetLoader`

`IAssetLoader` 定义后端的 `Load<T>` 和 `LoadAsync<T>` 契约，返回 `AssetLoadResult<T>`。当前由 `ResourcesAssetLoader` 和 `AddressablesAssetLoader` 实现。增加后端或测试替身时应保持此契约；业务调用方仍应使用 `AssetLoader`，避免依赖后端选择细节。

### `ResourcesAssetLoader`

使用 `Resources.Load<T>` 和 `Resources.LoadAsync<T>` 实现加载。Resources 未命中时，`AssetLoader` 会尝试 Addressables。该后端不会提供单项释放回调；句柄生命周期仍应遵守调用方的所有权规则。

### `AddressablesAssetLoader`

使用 `Addressables.LoadAssetAsync<T>` 加载，并在成功结果的释放回调中执行 `Addressables.Release`。同步 API 使用 `WaitForCompletion()`，可能阻塞主线程，因此只应在确实要求立即结果的低频流程使用；异步 API 是优先选择。

Addressables 操作句柄由资源层管理，不应泄漏给 Presenter、Proxy 或玩法代码。加载失败由后端记录警告并返回空结果，调用方需检查返回值。

## YooAsset 与 Addressables 怎么选

两者都能把资源从场景或业务代码中分离出来，支持按地址加载、异步加载和资源生命周期管理。主要差异在于：**Addressables 更像 Unity 官方的资源寻址与加载系统；YooAsset 更强调围绕 AssetBundle 的资源包构建、下载和版本更新流程。**

| 对比项 | Addressables | YooAsset |
| --- | --- | --- |
| 维护与工作流 | Unity 官方包，直接集成 Unity Editor、Group、Catalog 和 Profiler。 | 第三方开源方案，有自己的资源包配置、构建、运行模式和部署流程。 |
| 本地加载 | 通过 Address、Label、Group 等组织并加载资源。 | 通过 Package、资源地址和标签等组织并加载资源。 |
| 远端内容 | 支持远端 Bundle 和 Catalog；发布方仍需配置远端地址、构建内容更新并部署 Catalog/Bundle。 | 提供较完整的资源版本、清单、下载与补丁部署工作流，适合需要把更新管线作为产品能力维护的项目。 |
| 接入成本 | Unity 项目可渐进采用；需要自行设计项目的内容发布、下载提示和失败恢复流程。 | 资源更新能力较集中，但团队需要采用并维护它的初始化、Package、构建与部署规范。 |
| 更适合 | Unity 项目逐步迁移资源、以本地资源为主，或需要常规远端内容且希望使用官方工具链。 | 明确需要分包、下载队列、断点续传、补丁版本管理或更完整的热更新流程。 |

**本项目当前选择 Addressables。** 项目已安装 Addressables，并有 `AddressablesAssetLoader` 和本地 Group；YooAsset 尚未引入。当前仍处于 Resources 与 Addressables 并存的迁移状态，不代表已经接入远端热更新。现有 `AssetLoader` 将业务层与具体后端隔离，因此若未来出现多 Package、复杂下载/补丁管理等明确需求，再评估是否切换或扩展后端，而不是现在同时接入两套系统。

更完整的项目现状、切换条件与选型资料见 [资源管理系统的技术选型](../../../../Docs/ResourceManagement.md#41-技术选型addressables-与-yooasset)。

## 使用方式

### 同步加载

新代码即使同步加载，也应显式保存句柄，并在对应生命周期结束时释放：

```csharp
AssetHandle<GameObject> prefabHandle =
  AssetLoader.Instance.LoadHandle<GameObject>(
    "Prefabs/Survivor/View/SurvivorHome");

if (prefabHandle == null)
{
  Debug.LogError("[Home] Prefab 加载失败。");
  return;
}

try
{
  GameObject prefab = prefabHandle.Asset;
  // 在句柄存活期间使用资源。
}
finally
{
  prefabHandle.Dispose();
}
```

如果对象需要在当前方法返回后继续依赖资源，应将句柄保存在对象所有者中，并在该所有者销毁、关闭或清理时释放；不要在局部 `finally` 中提前释放。

### 异步加载

```csharp
private async Task<AssetHandle<GameObject>> LoadPrefabAsync(string assetKey)
{
  AssetHandle<GameObject> handle =
    await AssetLoader.Instance.LoadHandleAsync<GameObject>(assetKey);

  if (handle == null)
  {
    Debug.LogError($"[Home] Prefab 加载失败：{assetKey}");
    return null;
  }

  return handle;
}

AssetHandle<GameObject> handle = await LoadPrefabAsync(assetKey);
if (handle == null)
  return;

try
{
  GameObject prefab = handle.Asset;
  // 在句柄存活期间使用资源。
}
finally
{
  handle.Dispose();
}
```

如果在 `await` 期间界面或宿主可能被关闭，调用方必须在加载完成后重新检查其生命周期；必要时释放迟到的句柄并放弃后续操作。资源加载本身目前不支持取消。

### 共享给另一个所有者

`Retain()` 为新的所有者建立独立引用。双方分别在自己的生命周期结束时释放：

```csharp
private AssetHandle<GameObject> _poolPrefabHandle;

private void KeepPrefabForPool(AssetHandle<GameObject> prefabHandle)
{
  _poolPrefabHandle = prefabHandle.Retain();
}

private void ReleasePoolPrefab()
{
  _poolPrefabHandle?.Dispose();
  _poolPrefabHandle = null;
}
```

实际长期所有权应保存返回的句柄，而不是只保存 `Asset` 裸引用。项目中的对象池会自行 `Retain()`，不接管调用方传入的句柄。

## 生命周期与注意事项

1. 优先使用 `LoadHandle<T>` / `LoadHandleAsync<T>`；裸资源兼容 API 只用于迁移中的旧代码。
2. 句柄由创建它的所有者负责 `Dispose()`。UI、音频和对象池应在资源不再被其缓存或实例使用后释放。
3. 不要在持有者仍需访问资源时释放句柄；也不要仅为方便而把每个句柄延迟到 `ReleaseAll()` 才处理。
4. `ReleaseAll()` 会强制释放所有租约，即使调用方仍保留句柄，因此仅在全局销毁流程中、资源使用者已释放之后调用。
5. 并发请求合并只覆盖尚未完成的同键同类型加载，不代表完成后的跨调用缓存。
6. Resources 与 Addressables 使用相同资源键回退；应避免键冲突。键的具体规则以 [资源管理系统](../../../../Docs/ResourceManagement.md) 为准。
7. Unity 资源加载应从主线程发起；不要把 Unity API 放进任意线程执行的工作任务。

## 项目中的调用示例

- UI Presenter 保留 Prefab 句柄至 Presenter 销毁，见 [UIManager.cs](../MVC/UIManager.cs)。
- 音乐和音效按播放生命周期保留句柄，见 [AudioManager.cs](../Manager/AudioManager.cs)。
- 对象池预热后保留 Prefab 句柄至池退役且活跃对象清理，见 [PoolManager.cs](../Pool/PoolManager.cs)。
- `GameMgr` 先释放 UI 和对象池，再调用 `ReleaseAll()` 做全局兜底，见 [GameMgr.cs](../Launcher/GameMgr.cs)。
