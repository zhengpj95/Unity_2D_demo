# UIManager 架构说明

## 架构变更

### 之前：MonoBehaviour 单例

```csharp
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // UI层级作为序列化字段
    public Transform mainLayer;
    // ...
}
```

**问题：**
- 依赖 MonoBehaviour 生命周期
- 需要在场景中存在 GameObject
- 不易进行单元测试
- 配置和逻辑耦合

### 现在：纯 C# 单例 + 配置注入

```csharp
public class UIManager : Singleton<UIManager>
{
    private UIManagerConfig _config;

    public void Initialize(UIManagerConfig config)
    {
        _config = config;
        // ...
    }

    public void Release()
    {
        // 清理资源
    }
}
```

**优势：**
- ✅ 完全解耦，不依赖 MonoBehaviour
- ✅ 通过配置注入，支持多场景切换
- ✅ 易于单元测试
- ✅ 生命周期管理清晰
- ✅ 使用已有的 Singleton<T> 基类

## 使用方式

### 1. 场景配置

创建 UI 层级结构：

```
Canvas
├── MainLayer      (主界面层)
├── WindowLayer    (弹窗层)
├── ModelLayer     (模态层)
└── TipLayer       (提示层)
```

### 2. 添加 UILauncher

```
场景层级：
Launcher (挂载 UILauncher)
└── Canvas
    ├── MainLayer
    ├── WindowLayer
    ├── ModelLayer
    └── TipLayer
```

在 Inspector 中：
- 将各个 Layer 拖拽到 UILauncher 的对应字段
- 勾选 "Dont Destroy On Load"（可选）

### 3. 使用 API

```csharp
// 显示UI
UIManager.Instance.ShowUI("UI/Panel", UILayerIndex.Window);

// 隐藏UI
UIManager.Instance.HideUI("UI/Panel");

// 销毁UI
UIManager.Instance.DestroyUI("UI/Panel");

// 检查状态
bool isInit = UIManager.Instance.IsInitialized;
bool hasUI = UIManager.Instance.HasUI("UI/Panel");

// MVC集成
var controller = UIManager.Instance.ShowUIWithController
    <MyView, MyController, MyData>(
    "UI/Panel",
    UILayerIndex.Window,
    new MyData { Title = "Hello" }
);
```

## API 对比

### 基础功能

| 旧 API                  | 新 API                  | 说明           |
| ----------------------- | ----------------------- | -------------- |
| `ShowUI(path, layer)`   | `ShowUI(path, layer)`   | ✅ 保持一致     |
| `HideUI(path, destroy)` | `HideUI(path, destroy)` | ✅ 保持一致     |
| `GetUIObject(path)`     | `GetUIObject(path)`     | ✅ 保持一致     |
| `HasUI(path)`           | `HasUI(path)`           | ✅ 保持一致     |
| -                       | `IsInitialized`         | ✅ 新增状态检查 |
| -                       | `Release()`             | ✅ 新增资源释放 |

### 新增功能

| API                                    | 说明                 |
| -------------------------------------- | -------------------- |
| `Initialize(config)`                   | 初始化管理器         |
| `Initialize(main, window, model, tip)` | 便捷初始化           |
| `UpdateConfig(config)`                 | 更新配置（场景切换） |
| `Release()`                            | 释放资源             |
| `CachedUICount`                        | 获取缓存UI数量       |
| `GetAllCachedUI()`                     | 获取所有缓存UI路径   |

## 注意事项

### 1. 初始化顺序

确保在使用 UIManager 前完成初始化：

```csharp
// ✅ 正确
void Awake()
{
    UIManager.Instance.Initialize(config);
    UIManager.Instance.ShowUI(...);  // 可以使用
}

// ❌ 错误
void Awake()
{
    UIManager.Instance.ShowUI(...);  // 会警告未初始化
}
```

### 2. 场景切换

切换场景时更新配置：

```csharp
void OnSceneLoaded(Scene scene)
{
    // 查找新场景的层级
    var newConfig = new UIManagerConfig(
        FindLayer("MainLayer"),
        FindLayer("WindowLayer"),
        FindLayer("ModelLayer"),
        FindLayer("TipLayer")
    );

    // 更新配置
    UIManager.Instance.UpdateConfig(newConfig);
}
```

### 3. 资源清理

场景销毁时释放资源：

```csharp
void OnDestroy()
{
    if (UIManager.IsCreated)
    {
        UIManager.Instance.Release();
    }
}
```

## 性能优化

### 1. 预加载UI

```csharp
// 提前加载不立即显示的UI
IEnumerator PreloadUIs()
{
    yield return null;  // 等待一帧

    UIManager.Instance.ShowUI("UI/HeavyPanel", UILayerIndex.Window);
    UIManager.Instance.HideUI("UI/HeavyPanel", false);
}
```

### 2. 批量操作

```csharp
// 获取所有缓存的UI
foreach (var path in UIManager.Instance.GetAllCachedUI())
{
    Debug.Log($"Cached: {path}");
}
```

### 3. 内存管理

```csharp
// 销毁不用的UI
UIManager.Instance.DestroyUI("UI/UnusedPanel");

// 或清空所有
UIManager.Instance.DestroyAllUI();
```

## 总结

纯 C# 的 UIManager 设计提供了：
- 更好的架构解耦
- 更灵活的配置管理
- 更清晰的生命周期
- 更好的可测试性

通过 UILauncher 组件，在保持易用性的同时，获得了更好的架构设计。