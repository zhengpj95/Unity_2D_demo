# Unity MVC UI Framework

一个轻量级的Unity UI MVC框架，用于管理Unity界面、弹窗等UI组件。

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
   - 管理UI数据
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
│   ├── UIData.cs        # 数据基类
│   ├── UIModel.cs       # Model基类
│   ├── UIView.cs        # View基类
│   └── UIController.cs  # Controller基类
├── Manager/             # 管理器
│   ├── UIControllerManager.cs
│   └── UIManagerExtensions.cs
├── Utils/               # 工具类
│   └── UIBinding.cs     # 数据绑定工具
└── Examples/            # 示例代码
    ├── ConfirmDialogView.cs
    ├── ConfirmDialogController.cs
    └── MVCUsageExample.cs
```

## 快速开始

### 1. 定义数据模型

```csharp
[Serializable]
public class MyUIData : UIData
{
    public string Title;
    public int Score;

    public override void Reset()
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
    protected override UIModel<MyUIData> CreateModel()
    {
        return new UIModel<MyUIData>();
    }

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

## 内置数据类型

- **DialogData**: 弹窗数据（标题、内容、按钮回调）
- **MessageData**: 提示消息数据（消息内容、持续时间、消息类型）
- **SimpleData\<T\>**: 简单值类型数据

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
var controller = UIManager.Instance.ShowUIWithController<MyView, MyController, MyData>(
    "UI/MyUI",
    UILayerIndex.Window,
    new MyData { Title = "Test" }
);
```

## 注意事项

1. View必须继承自UIView\<TData\>并实现UpdateView方法
2. Controller必须继承自UIController\<TData\>并实现CreateModel方法
3. 数据类必须继承自UIData
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

- v1.0.0 - 初始版本，包含核心MVC架构和基础示例