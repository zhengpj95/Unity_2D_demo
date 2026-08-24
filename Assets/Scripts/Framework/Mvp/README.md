# MVP 模块框架

本目录提供按业务模块组织的 MVP 架构。`ModuleManager` 是唯一模块入口；每个 `BaseModule` 通过唯一的 `ModuleName` 持有自己的 Command、Proxy 与 Presenter。

## 职责划分

| 类型 | 职责 | 不应承担的职责 |
| --- | --- | --- |
| `ModuleManager` | 模块注册、初始化、逐帧更新、释放 | 具体业务逻辑 |
| `BaseModule` | 聚合本模块组件、注册事件并管理其生命周期 | 跨模块直接操作内部状态 |
| `BaseCommand` | 执行事件对应的业务流程 | 自行订阅 EventBus、持久化数据、直接处理网络协议 |
| `BaseProxy` | 数据状态、业务数据操作、协议注册/回调 | 界面显示和按钮逻辑 |
| `BasePresenter` | View 生命周期、界面交互与展示 | 协议收发和跨业务决策 |

## 生命周期

```text
PushModules / RegisterModule
            ↓
       InitializeAll
            ↓
Module.OnInit（注册 Proxy / Command）
            ↓
Proxy.OnInit（注册协议） → Command.Execute（由 Module 的事件监听触发）
            ↓
          Update
            ↓
ReleaseModule / ReleaseAll
            ↓
Command 取消事件 → Proxy 取消协议 → Presenter 销毁 → Module.OnRelease
```

`RegisterModule` 在 `ModuleManager.InitializeAll()` 之后调用时，会立即初始化新模块；`PushModules<T>()` 则始终延迟到下一次 `InitializeAll()` 创建。

## 新建模块

先为模块添加唯一枚举值：

```csharp
public enum ModuleName
{
  None = 0,
  Login = 1,
  Bag = 3,
  Shop = 4,
}
```

然后实现模块，并只在 `OnInit` 中登记它拥有的对象：

```csharp
public sealed class ShopModule : BaseModule
{
  public override ModuleName ModuleName => ModuleName.Shop;

  protected override void OnInit()
  {
    RegProxy<ShopProxy>();
    RegCmd<OpenShopCommand>("shop.open");
  }
}
```

启动时可延迟注册：

```csharp
ModuleManager.Instance.PushModules<LoginModule>();
ModuleManager.Instance.PushModules<ShopModule>();
ModuleManager.Instance.InitializeAll();
```

## 事件命令示例

Module 通过统一的 `RegCmd<TCommand>(eventName)` 将事件和 Command 绑定，Command 实例由 BaseModule 内部创建；参数类型不需要在注册时声明，框架会在模块释放时自动取消订阅。Command 不负责事件监听，只实现 `Execute`。

```csharp
public sealed class OpenShopCommand : BaseCommand
{
  public override void Execute(object args = null)
  {
    // 打开界面或调用本模块 Proxy。
  }
}

// 在 ShopModule.OnInit 中：
RegCmd<OpenShopCommand>("shop.open");
RegCmd<SelectShopItemCommand>("shop.select_item");
```

对应派发：`EventBus.Dispatch("shop.open");` 或 `EventBus.Dispatch("shop.select_item", itemId);`。同一事件名的有参/无参版本不可混用，因为 EventBus 会按委托类型分发。

## Proxy 与 UI 约定

- 在 `BaseProxy.OnInit` 内使用 `RegisterHandler` 注册协议；同一 Proxy 重复注册同一个协议号会抛出异常，模块释放时会自动注销。
- 使用 `RegPresenter` 登记已有 Presenter，或使用模块的 `OpenWindow<T>` 打开并自动登记 Presenter；模块释放时会自动销毁。
- `GetProxy<T>()`、`GetCommand<T>()` 与 `GetPresenter<T>()` 只用于访问本模块组件；跨模块访问必须先经 `ModuleManager.GetModule<T>(ModuleName)`，避免隐式依赖。
- 不要在 `OnRelease` 后缓存或继续使用 Command、Proxy、Presenter 引用。
