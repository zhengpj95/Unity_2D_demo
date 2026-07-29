using System;
using System.Collections.Generic;
using UnityEngine;

// UI 层级定义
public enum UILayer
{
  Normal = 0,
  Fixed = 100,
  PopUp = 200,
  Toast = 300
}

// UI 视图基类 (挂载在 UI Prefab 上)
public abstract class UIView : MonoBehaviour
{
  // 用于组件引用的绑定（也可以在 Inspector 中拖拽）
  public virtual void InitView()
  {
  }
}

// UI 控制器/逻辑基类 (纯 C# 类)
public abstract class UIPresenter
{
  public UIView View { get; private set; }
  public bool IsVisible { get; private set; }

  // 初始化（加载 Prefab 后调用一次）
  public virtual void OnInit(UIView view)
  {
    View = view;
  }

  // 打开界面
  public virtual void OnOpen(object args = null)
  {
    IsVisible = true;
    if (View != null) View.gameObject.SetActive(true);
  }

  // 关闭界面
  public virtual void OnClose()
  {
    IsVisible = false;
    if (View != null) View.gameObject.SetActive(false);
  }

  // 销毁界面
  public virtual void OnDestroy()
  {
    if (View != null)
    {
      UnityEngine.Object.Destroy(View.gameObject);
      View = null;
    }
  }
}

public class UIManagerNew : MonoBehaviour
{
  public static UIManagerNew Instance { get; private set; }

  [Header("UI 根节点设置")]
  public Transform normalRoot;
  public Transform fixedRoot;
  public Transform popUpRoot;
  public Transform toastRoot;

  // 保存所有已实例化的 Presenter
  private readonly Dictionary<Type, UIPresenter> _presenterCache = new Dictionary<Type, UIPresenter>();
  // 界面打开栈 (主要用于 PopUp 层管理)
  private readonly Stack<UIPresenter> _uiStack = new Stack<UIPresenter>();

  private void Awake()
  {
    if (Instance == null) Instance = this;
    else Destroy(gameObject);
  }

  /// <summary>
  /// 打开界面
  /// </summary>
  public T OpenWindow<T>(string prefabPath, UILayer layer, object args = null) where T : UIPresenter, new()
  {
    Type type = typeof(T);
    if (!_presenterCache.TryGetValue(type, out UIPresenter presenter))
    {
      // 1. 模拟异步/同步加载 Prefab (实际项目中可替换为 Addressables / AssetBundle)
      GameObject prefab = Resources.Load<GameObject>(prefabPath);
      Transform parent = GetLayerRoot(layer);
      GameObject go = Instantiate(prefab, parent);

      UIView view = go.GetComponent<UIView>();

      // 2. 实例化 Presenter 并初始化
      presenter = new T();
      presenter.OnInit(view);
      _presenterCache.Add(type, presenter);
    }

    // 3. 入栈控制（如果是 PopUp 类型的弹窗，可以压栈管理）
    if (layer == UILayer.PopUp)
    {
      _uiStack.Push(presenter);
    }

    // 4. 调用 Open 生命周期
    presenter.OnOpen(args);
    return (T)presenter;
  }

  /// <summary>
  /// 关闭指定界面
  /// </summary>
  public void CloseWindow<T>() where T : UIPresenter
  {
    Type type = typeof(T);
    if (_presenterCache.TryGetValue(type, out UIPresenter presenter))
    {
      if (presenter.IsVisible)
      {
        presenter.OnClose();
      }
    }
  }

  /// <summary>
  /// 出栈 (关闭最顶层的弹窗)
  /// </summary>
  public void PopWindow()
  {
    if (_uiStack.Count > 0)
    {
      UIPresenter topPresenter = _uiStack.Pop();
      topPresenter.OnClose();
    }
  }

  private Transform GetLayerRoot(UILayer layer)
  {
    switch (layer)
    {
      case UILayer.Normal: return normalRoot;
      case UILayer.Fixed: return fixedRoot;
      case UILayer.PopUp: return popUpRoot;
      case UILayer.Toast: return toastRoot;
      default: return normalRoot;
    }
  }
}