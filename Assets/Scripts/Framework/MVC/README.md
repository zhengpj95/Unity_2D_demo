# Unity MVC UI Framework

一个轻量级的Unity UI MVC框架，用于管理Unity界面、弹窗等UI组件。

> **注意：** 本框架的所有类都在全局命名空间中，无需添加命名空间引用即可使用。

## 快速开始

### 1. 场景设置

在场景中创建 UI 层级结构：

```
Canvas
├── MainLayer      (主界面层)
├── WindowLayer    (弹窗层)
├── ModelLayer     (模态层)
└── TipLayer       (提示层)
```

### 2. 初始化 UIManager

**方式一：使用 UILauncher（推荐）**

1. 创建一个空 GameObject，命名为 "Launcher"
2. 挂载 `UILauncher` 脚本
3. 在 Inspector 中分配 UI 层级引用

**方式二：手动初始化**

```csharp
public class GameInitializer : MonoBehaviour
{
    [SerializeField] private Transform mainLayer;
    [SerializeField] private Transform windowLayer;
    [SerializeField] private Transform modelLayer;
    [SerializeField] private Transform tipLayer;

    private void Awake()
    {
        UIManager.Instance.Initialize(mainLayer, windowLayer, modelLayer, tipLayer);
    }

    private void OnDestroy()
    {
        UIManager.Instance.Release();
    }
}
```

### 3. 使用 UIManager

```csharp
// 显示UI
UIManager.Instance.ShowUI("UI/MyPanel", UILayerIndex.Window);

// 隐藏UI
UIManager.Instance.HideUI("UI/MyPanel");

// 销毁UI
UIManager.Instance.DestroyUI("UI/MyPanel");
```

## 架构概览

```
┌─────────────────────────────────────────────────────────────┐
│                         UI System                           │
├─────────────────────────────────────────────────────────────┤
│  用户交互 ←─── View ───→ Controller ←─── Model ───→ 数据   │
└─────────────────────────────────────────────────────────────┘
```

### 核心组件

1. **Model（模型层）**
   - 管理模块数据
   - 数据变化通知
   - 业务逻辑处理

2. **View（视图层）**
   - UI显示和渲染
   - 用户交互处理
   - 组件引用缓存

3. **Controller（控制器层）**
   - 连接Model和View
   - 处理业务逻辑
   - 管理生命周期

## 目录结构

```
Assets/Scripts/Framework/MVC/
├── Interfaces/          # 接口定义
│   └── IUIBase.cs
├── Base/                # 基类实现
│   ├── UIModel.cs       # Model基类
│   ├── UIView.cs        # View基类
│   └── UIController.cs  # Controller基类
├── Manager/             # 管理器
│   ├── UIControllerManager.cs
│   └── UIManagerExtensions.cs
├── Utils/               # 工具类
│   └── UIBinding.cs     # 数据绑定工具
└── Examples/            # 示例代码
    ├── DialogData.cs
    ├── ConfirmDialogView.cs
    ├── ConfirmDialogController.cs
    └── MVCUsageExample.cs
```

## 快速开始

### 1. 定义数据模型

**方式一：简单数据类（推荐）**

数据类不需要继承任何基类，可以自由定义：

```csharp
[Serializable]
public class MyUIData
{
    public string Title;
    public int Score;
}
```

**方式二：实现 IUIData 接口（可选）**

如果需要 Reset 功能，可以实现 `IUIData` 接口：

```csharp
[Serializable]
public class MyUIData : IUIData
{
    public string Title;
    public int Score;

    public void Reset()
    {
        Title = string.Empty;
        Score = 0;
    }
}
```

### 2. 创建View

```csharp
public class MyUIView : UIView<MyUIData>
{
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _scoreText;

    public override void UpdateView(MyUIData data)
    {
        UIBinding.BindText(_titleText, data.Title);
        UIBinding.BindText(_scoreText, $"Score: {data.Score}");
    }
}
```

### 3. 创建Controller

```csharp
public class MyUIController : UIController<MyUIData>
{
    // 使用 SimpleModel（推荐，简单场景）
    protected override UIModel<MyUIData> CreateModel()
    {
        return new SimpleModel<MyUIData>();
    }

    // 或创建自定义 Model（复杂场景）
    // protected override UIModel<MyUIData> CreateModel()
    // {
    //     return new MyCustomModel();
    // }

    public void UpdateScore(int newScore)
    {
        ModifyData(data => data.Score = newScore);
    }
}
```

### 4. 使用

```csharp
// 实例化UI
var go = Instantiate(uiPrefab);
var view = go.GetComponent<MyUIView>();

// 创建Controller
var controller = new MyUIController();
controller.Initialize(view);
controller.Start();

// 更新数据
controller.UpdateData(new MyUIData { Title = "Hello", Score = 100 });

// 显示/隐藏
controller.ShowView();
controller.HideView();
```

## 核心功能

### Model 类型

框架提供两种 Model 使用方式：

1. **SimpleModel\<TData\>**（推荐，简单场景）
   - 直接使用，无需继承
   - 适合简单的数据管理场景

```csharp
// 在 Controller 中直接使用
protected override UIModel<MyData> CreateModel()
{
    return new SimpleModel<MyData>();
}
```

2. **自定义 Model**（复杂场景）
   - 继承 `UIModel<TData>` 创建自定义 Model
   - 适合需要额外业务逻辑的场景

```csharp
public class MyCustomModel : UIModel<MyData>
{
    protected override void OnInit()
    {
        // 自定义初始化逻辑
    }

    public void DoSomething()
    {
        // 自定义业务逻辑
    }
}
```

### 数据绑定

```csharp
// 绑定文本
UIBinding.BindText(textComponent, "Hello World");

// 绑定按钮
UIBinding.BindButton(button, () => Debug.Log("Clicked"));

// 绑定Toggle
UIBinding.BindToggle(toggle, isOn, value => Debug.Log(value));

// 绑定Slider
UIBinding.BindSlider(slider, 0.5f, value => Debug.Log(value));
```

### 事件管理

```csharp
// 使用UIEventBinder集中管理事件
var binder = new UIEventBinder();
binder.BindButton(button, OnClick);
binder.BindSlider(slider, OnValueChanged);

// 清理所有绑定
binder.Clear();
```

### 组件缓存

```csharp
// 使用UIComponentCache缓存组件引用
var cache = new UIComponentCache(transform);
var text = cache.Get<Text>("Path/To/Text");
```

### Controller管理

```csharp
// 注册到管理器
UIControllerManager.Instance.Register<MyController, MyData>(controller);

// 获取Controller
var controller = UIControllerManager.Instance.Get<MyController>();

// 使用工厂方法创建
var controller = UIControllerFactory.Create<MyController, MyData>(view);
```

## 数据设计理念

本框架的设计理念是让每个模块的数据由对应的 Model 独立管理：

- **数据类无需继承 UIData**：模块数据类可以自由定义，不需要继承任何基类
- **IUIData 接口可选**：如果需要 Reset 功能，可以实现 `IUIData` 接口，但不是强制的
- **数据定义灵活**：每个模块可以根据自己的需求定义数据结构

## 最佳实践

1. **分离关注点**: Model只管数据，View只管显示，Controller处理逻辑
2. **使用UIBinding**: 避免手动查找组件和绑定事件
3. **生命周期管理**: 在合适的时机调用Initialize、Start、Stop、Cleanup
4. **事件清理**: 在OnDestroy或Cleanup中清理事件订阅
5. **数据不可变**: 通过ModifyData或UpdateData修改数据，避免直接修改

## 示例场景

查看 `Examples` 目录下的完整示例：

- **ConfirmDialog**: 确认弹窗实现
- **MVCUsageExample**: 完整使用流程演示

## 扩展UIManager

框架提供了UIManager的扩展方法，可以直接集成MVC：

```csharp
// 显示UI并自动创建Controller
var controller = UIManager.Instance.ShowUIWithController
    <MyView, MyController, MyData>(
    "UI/MyPanel",
    UILayerIndex.Window,
    new MyData { Message = "Test" }
);
```

## 架构设计

### 纯C#设计

UIManager 采用纯 C# 实现，具有以下优势：

1. **解耦合**：不依赖 MonoBehaviour，逻辑更清晰
2. **易测试**：可以编写单元测试，无需运行 Unity 场景
3. **灵活初始化**：通过配置注入，支持场景切换
4. **生命周期管理**：明确的 Initialize/Release 流程

### 初始化流程

```
场景启动
    ↓
UILauncher.Awake()
    ↓
UIManager.Instance.Initialize(config)
    ↓
UIManager 就绪，可正常使用
    ↓
场景销毁
    ↓
UILauncher.OnDestroy()
    ↓
UIManager.Instance.Release()
    ↓
清理所有资源
```

## 注意事项

1. View必须继承自UIView\<TData\>并实现UpdateView方法
2. Controller必须继承自UIController\<TData\>并实现CreateModel方法
3. 数据类不需要继承任何基类（IUIData接口是可选的）
4. 建议使用UIEventBinder管理事件，避免内存泄漏

## 依赖

- UnityEngine
- UnityEngine.UI
- TextMeshPro（可选）

## 为什么保持分离？

UIManager 和 UIManagerExtensions 保持分离的原因：

1. **编译顺序**：Unity 需要先编译 MVC 框架，然后才能编译依赖它的扩展方法
2. **低耦合**：UIManager 不强制依赖 MVC 框架，项目可以选择性使用
3. **单一职责**：UIManager 专注基础UI管理，Extensions 专注MVC集成
4. **向后兼容**：不使用 MVC 的项目不受影响

## 版本历史

- v1.1.0 - 移除 UIData 基类，数据类可自由定义
- v1.0.0 - 初始版本，包含核心MVC架构和基础示例