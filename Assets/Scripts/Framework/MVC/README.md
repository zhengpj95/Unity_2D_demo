# Module / MVP 业务框架

本目录提供按业务域组织的 Module + Presenter/View 框架。`ModuleManager` 是 Module 的唯一入口；每个 `BaseModule` 通过 `ModuleName` 聚合自己的 Proxy、Command 和 Presenter 定义。

## 职责

| 类型 | 负责 | 不负责 |
| --- | --- | --- |
| `ModuleManager` | 注册、初始化、Update 驱动和释放 Module | 具体业务规则 |
| `BaseModule` | 模块装配、组件查询、业务入口和生命周期收口 | 隐式初始化其他模块、保存所有场景瞬时状态 |
| `BaseProxy` | 模块数据、数据操作、协议 Handler 生命周期 | 直接操作 View |
| `BaseCommand` | 响应一个 EventBus 事件并编排一次业务动作 | 持久化状态、直接处理底层协议 |
| `BasePresenter` | View 创建后的生命周期、交互与展示 | 作为业务状态真源、直接处理协议 |
| `UIView` | Unity 组件引用和显示状态 | 业务流程编排 |

`BaseEmitter` 为 Module、Proxy、Command、Presenter 封装 EventBus 订阅。它记录当前对象通过 `On` 创建的监听，并在对应释放/关闭阶段由 `OffAll` 清理；没有第二套事件系统。

## 生命周期

```text
GameMgr.Awake
  → PushModules<T>()
  → ModuleManager.InitializeAll()
      → Module.OnInit()：登记 Proxy / Command / Presenter
      → Proxy.OnInit()：注册协议

GameMgr.Update
  → ModuleManager.Update()
      → 运行中 Module.OnUpdate()

GameMgr.OnDestroy
  → ModuleManager.ReleaseAll()
      → Module 先停止运行并解除自身事件
      → Proxy 注销协议并释放
      → 已实例化 Presenter 销毁
      → Module.OnRelease()
      → Command 释放
```

`RegisterModule` 在 Manager 已初始化后调用会立即初始化模块；`PushModules<T>()` 只登记类型，下一次 `InitializeAll()` 才创建。当前启动模块以 `GameMgr.InitializeModules()` 为准。

## 新建模块

先在 `Assets/Scripts/Define/ModuleName.cs` 追加唯一枚举值；如有窗口，再在 `ViewType.cs` 增加模块自己的 ViewType。模块只在 `OnInit` 登记其拥有的对象。当前最小参考实现是：

```csharp
public sealed class LoginModule : BaseModule
{
  public override ModuleName ModuleName => ModuleName.Login;

  protected override void OnInit()
  {
    RegProxy<LoginProxy>();
    RegCmd<LoginCmd>(EventDefine.TEST_LOGIN_COMMAND);
  }
}
```

需要 UI 时再登记 `RegPresenter<TPresenter>(viewType)`。新枚举值和事件 ID 应追加在末尾，避免改变已有序列化枚举值或运行期编号顺序。

## Command 与 EventBus

事件 ID 集中定义在 `Assets/Scripts/Define/EventDefine.cs`，类型为 `int`。所有监听器统一接收 `EventContext`；有参和无参事件可以使用同一 ID，监听方通过 `HasData`、`TryGetData<T>` 或 `GetData<T>` 读取数据。

```csharp
public sealed class LoginCmd : BaseCommand
{
  public override void Execute(EventContext context)
  {
    if (context.TryGetData(out string account))
    {
      // 使用参数执行一次业务动作。
    }
  }
}

// Module.OnInit
RegCmd<LoginCmd>(EventDefine.TEST_LOGIN_COMMAND);

// 调用方
EventBus.Emit(EventDefine.TEST_LOGIN_COMMAND, account);
```

直接使用 EventBus 时必须提供非空 owner：

```csharp
EventBus.On(EventDefine.FROG_HEALTH_CHANGED, OnHealthChanged, this);
EventBus.Off(EventDefine.FROG_HEALTH_CHANGED, OnHealthChanged, this);
```

MonoBehaviour 通常在 `OnEnable`/`OnDisable` 成对订阅；继承 `BaseEmitter` 的纯 C# 对象优先使用受保护的 `On`/`Emit`，并遵循框架的关闭或释放阶段。不要使用数字字面量作为事件 ID。

## Proxy 与协议

Proxy 在 `OnInit` 中通过 `RegisterHandler<T>(uint command, Action<T> handler)` 注册协议。每个 Proxy 独立记录自己注册的 cmd；重复注册会抛异常，模块释放时自动注销。

Proxy 可以更新本模块状态或派发业务事件，但不能直接访问 View。网络数据流与限制见 `../Network/README.md`。

## Presenter / View

模块用 `RegPresenter<TPresenter>(viewType)` 建立一一映射；第一次调用 `OpenWindow<TPresenter>(viewType, args)` 时，`UIManager` 才创建并缓存 Presenter/View。

- Presenter 的 `Layer` 决定 `Main/Window/Model/Tip` 层级。
- `PrefabPath` 是传给 `AssetLoader` 的稳定资源键，历史命名仍保留为 Path。
- `ModuleViewKey(ModuleName, ViewType)` 是全局缓存身份。
- `GetPresenter(viewType)` 只返回已经实例化的 Presenter，未打开时返回 `null`。
- `UIManager.HidePresenter` 隐藏并保留缓存；`CloseWindow` 会关闭并销毁 Presenter。
- `OnClose` 会解除 `BaseEmitter` 事件，`OnDestroy` 还会清理按钮监听和 View 实例。

需要强类型参数时使用 `BasePresenter<TView, TArgs>`，其中 `TArgs` 必须为 `struct`。界面关闭、异步返回和场景切换时应重新检查目标对象是否仍有效。

## 跨模块规则

- 本模块内通过 `GetProxy<T>()`、`GetCommand<T>()`、`GetPresenter(viewType)` 查询。
- 跨模块先通过 `ModuleManager.GetModule<T>(ModuleName)` 获取明确入口，或使用稳定接口/事件。
- 不缓存已经释放的 Module、Proxy、Command 或 Presenter 引用。
- 不用 Singleton/场景查找隐藏循环依赖，也不为简单的一对一调用滥用全局 EventBus。

项目级关系、UIManager 资源语义和当前模块清单见 `Docs/Architecture.md`。
