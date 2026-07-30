using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// UI View基类
/// 负责UI显示和用户交互
/// 绑定到UI GameObject上，作为UI的入口点
/// </summary>
/// <typeparam name="TData">数据类型</typeparam>
public abstract class UIView<TData> : MonoBehaviour, IView<TData>
{
  [Header("View Settings")]
  [SerializeField] private bool _autoFindComponents = true;
  [SerializeField] private bool _hideOnStart = true;

  /// <summary>
  /// 显示状态变化事件
  /// </summary>
  public event Action<bool> OnVisibilityChanged;

  /// <summary>
  /// 获取对应的GameObject
  /// </summary>
  public GameObject GameObject => gameObject;

  /// <summary>
  /// 是否已初始化
  /// </summary>
  public bool IsInitialized { get; protected set; }

  /// <summary>
  /// 是否可见
  /// </summary>
  public bool IsVisible => gameObject.activeSelf;

  /// <summary>
  /// Canvas组件（可选）
  /// </summary>
  protected Canvas _canvas;

  /// <summary>
  /// CanvasGroup组件（可选，用于淡入淡出）
  /// </summary>
  protected CanvasGroup _canvasGroup;

  /// <summary>
  /// Animator组件（可选，用于动画）
  /// </summary>
  protected Animator _animator;

  /// <summary>
  /// 初始化视图
  /// </summary>
  public virtual void Initialize()
  {
    if (IsInitialized) return;

    // 自动查找组件
    if (_autoFindComponents)
    {
      CacheComponents();
    }

    // 绑定UI事件
    BindUIEvents();

    // 初始化子类
    OnInit();

    IsInitialized = true;

    // 默认隐藏
    if (_hideOnStart)
    {
      gameObject.SetActive(false);
    }
  }

  /// <summary>
  /// 缓存组件
  /// </summary>
  protected virtual void CacheComponents()
  {
    _canvas = GetComponent<Canvas>();
    _canvasGroup = GetComponent<CanvasGroup>();
    _animator = GetComponent<Animator>();
  }

  /// <summary>
  /// 绑定UI事件（子类实现）
  /// </summary>
  protected virtual void BindUIEvents() { }

  /// <summary>
  /// 初始化方法（子类重写）
  /// </summary>
  protected virtual void OnInit() { }

  /// <summary>
  /// 更新视图显示
  /// </summary>
  /// <param name="data">数据</param>
  public abstract void UpdateView(TData data);

  /// <summary>
  /// 显示视图
  /// </summary>
  public virtual void Show()
  {
    if (!IsInitialized)
    {
      Initialize();
    }

    gameObject.SetActive(true);
    OnShowing();
    OnVisibilityChanged?.Invoke(true);
  }

  /// <summary>
  /// 隐藏视图
  /// </summary>
  public virtual void Hide()
  {
    OnHiding();
    gameObject.SetActive(false);
    OnVisibilityChanged?.Invoke(false);
  }

  /// <summary>
  /// 显示时调用（子类可重写）
  /// </summary>
  protected virtual void OnShowing() { }

  /// <summary>
  /// 隐藏时调用（子类可重写）
  /// </summary>
  protected virtual void OnHiding() { }

  /// <summary>
  /// 清理视图
  /// </summary>
  public virtual void Cleanup()
  {
    OnCleanup();
    OnVisibilityChanged = null;
    IsInitialized = false;
  }

  /// <summary>
  /// 清理方法（子类重写）
  /// </summary>
  protected virtual void OnCleanup() { }

  #region 辅助方法

  /// <summary>
  /// 安全获取组件
  /// </summary>
  protected T GetSafeComponent<T>(string path = null) where T : Component
  {
    T component = null;

    if (string.IsNullOrEmpty(path))
    {
      component = GetComponent<T>();
    }
    else
    {
      var child = transform.Find(path);
      if (child != null)
      {
        component = child.GetComponent<T>();
      }
    }

    if (component == null)
    {
      Debug.LogWarning($"[{GetType().Name}] Component not found: {typeof(T).Name} at path: {path}");
    }

    return component;
  }

  /// <summary>
  /// 查找子对象
  /// </summary>
  protected Transform FindChild(string path)
  {
    var child = transform.Find(path);
    if (child == null)
    {
      Debug.LogWarning($"[{GetType().Name}] Child not found: {path}");
    }
    return child;
  }

  /// <summary>
  /// 添加按钮点击事件
  /// </summary>
  protected void AddButtonClickListener(string path, Action onClick)
  {
    var btn = FindChild(path)?.GetComponent<Button>();
    if (btn != null)
    {
      btn.onClick.AddListener(() => onClick?.Invoke());
    }
  }

  /// <summary>
  /// 添加按钮点击事件（通过Button组件）
  /// </summary>
  protected void AddButtonClickListener(Button button, Action onClick)
  {
    if (button != null)
    {
      button.onClick.AddListener(() => onClick?.Invoke());
    }
  }

  /// <summary>
  /// 设置文本内容
  /// </summary>
  protected void SetText(string path, string text)
  {
    var txt = FindChild(path)?.GetComponent<Text>();
    if (txt != null)
    {
      txt.text = text;
    }
  }

  /// <summary>
  /// 设置文本内容（TMPro版本）
  /// </summary>
  protected void SetTextTMPro(string path, string text)
  {
#if UNITY_TEXTMESHPRO
        var txt = FindChild(path)?.GetComponent<TMPro.TMP_Text>();
        if (txt != null)
        {
            txt.text = text;
        }
#endif
  }

  /// <summary>
  /// 设置图像
  /// </summary>
  protected void SetImage(string path, Sprite sprite)
  {
    var img = FindChild(path)?.GetComponent<Image>();
    if (img != null)
    {
      img.sprite = sprite;
    }
  }

  /// <summary>
  /// 设置激活状态
  /// </summary>
  protected void SetActive(string path, bool active)
  {
    var child = FindChild(path);
    if (child != null)
    {
      child.gameObject.SetActive(active);
    }
  }

  #endregion

  #region Unity生命周期

  protected virtual void Awake()
  {
    // 自动初始化（可选）
    // Initialize();
  }

  protected virtual void OnDestroy()
  {
    Cleanup();
  }

  #endregion
}