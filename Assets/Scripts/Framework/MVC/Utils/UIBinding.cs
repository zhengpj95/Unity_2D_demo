using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI数据绑定工具
/// 提供简化的数据绑定功能
/// </summary>
public static class UIBinding
{
  /// <summary>
  /// 绑定文本
  /// </summary>
  public static void BindText(Text text, string value)
  {
    if (text != null)
    {
      text.text = value ?? string.Empty;
    }
  }

  /// <summary>
  /// 绑定文本（带格式化）
  /// </summary>
  public static void BindText(Text text, string format, params object[] args)
  {
    if (text != null)
    {
      text.text = string.Format(format ?? "{0}", args);
    }
  }

#if UNITY_TEXTMESHPRO
    /// <summary>
    /// 绑定TextMeshPro文本
    /// </summary>
    public static void BindText(TMPro.TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }

    /// <summary>
    /// 绑定TextMeshPro文本（带格式化）
    /// </summary>
    public static void BindText(TMPro.TMP_Text text, string format, params object[] args)
    {
        if (text != null)
        {
            text.text = string.Format(format ?? "{0}", args);
        }
    }
#endif

  /// <summary>
  /// 绑定图像
  /// </summary>
  public static void BindImage(Image image, Sprite sprite)
  {
    if (image != null)
    {
      image.sprite = sprite;
    }
  }

  /// <summary>
  /// 绑定图像（带颜色）
  /// </summary>
  public static void BindImage(Image image, Sprite sprite, Color color)
  {
    if (image != null)
    {
      image.sprite = sprite;
      image.color = color;
    }
  }

  /// <summary>
  /// 绑定按钮点击事件
  /// </summary>
  public static void BindButton(Button button, Action onClick)
  {
    if (button != null && onClick != null)
    {
      button.onClick.RemoveAllListeners();
      button.onClick.AddListener(() => onClick());
    }
  }

  /// <summary>
  /// 添加按钮点击事件（不移除之前的监听器）
  /// </summary>
  public static void AddButtonClick(Button button, Action onClick)
  {
    if (button != null && onClick != null)
    {
      button.onClick.AddListener(() => onClick());
    }
  }

  /// <summary>
  /// 绑定Toggle
  /// </summary>
  public static void BindToggle(Toggle toggle, bool isOn, Action<bool> onValueChanged = null)
  {
    if (toggle != null)
    {
      toggle.isOn = isOn;
      if (onValueChanged != null)
      {
        toggle.onValueChanged.AddListener(value => onValueChanged(value));
      }
    }
  }

  /// <summary>
  /// 绑定Slider
  /// </summary>
  public static void BindSlider(Slider slider, float value, Action<float> onValueChanged = null)
  {
    if (slider != null)
    {
      slider.value = value;
      if (onValueChanged != null)
      {
        slider.onValueChanged.AddListener(v => onValueChanged(v));
      }
    }
  }

  /// <summary>
  /// 绑定InputField
  /// </summary>
  public static void BindInputField(InputField inputField, string value, Action<string> onValueChanged = null, Action<string> onEndEdit = null)
  {
    if (inputField != null)
    {
      inputField.text = value ?? string.Empty;
      if (onValueChanged != null)
      {
        inputField.onValueChanged.AddListener(v => onValueChanged(v));
      }
      if (onEndEdit != null)
      {
        inputField.onEndEdit.AddListener(v => onEndEdit(v));
      }
    }
  }

  /// <summary>
  /// 绑定Dropdown
  /// </summary>
  public static void BindDropdown(Dropdown dropdown, int value, Action<int> onValueChanged = null)
  {
    if (dropdown != null)
    {
      dropdown.value = value;
      if (onValueChanged != null)
      {
        dropdown.onValueChanged.AddListener(v => onValueChanged(v));
      }
    }
  }

  /// <summary>
  /// 绑定激活状态
  /// </summary>
  public static void BindActive(GameObject obj, bool active)
  {
    if (obj != null)
    {
      obj.SetActive(active);
    }
  }

  /// <summary>
  /// 绑定激活状态（Transform）
  /// </summary>
  public static void BindActive(Transform transform, bool active)
  {
    if (transform != null)
    {
      transform.gameObject.SetActive(active);
    }
  }
}

/// <summary>
/// UI事件绑定器
/// 用于集中管理UI事件绑定，支持一次性清理
/// </summary>
public class UIEventBinder
{
  private readonly List<Action> _cleanupActions = new List<Action>();

  /// <summary>
  /// 绑定按钮点击事件
  /// </summary>
  public void BindButton(Button button, Action onClick)
  {
    if (button == null || onClick == null) return;

    button.onClick.AddListener(InvokeOnClick);
    _cleanupActions.Add(() => button.onClick.RemoveListener(InvokeOnClick));

    void InvokeOnClick() => onClick();
  }

  /// <summary>
  /// 绑定Toggle事件
  /// </summary>
  public void BindToggle(Toggle toggle, Action<bool> onValueChanged)
  {
    if (toggle == null || onValueChanged == null) return;

    toggle.onValueChanged.AddListener(InvokeOnValueChanged);
    _cleanupActions.Add(() => toggle.onValueChanged.RemoveListener(InvokeOnValueChanged));

    void InvokeOnValueChanged(bool value) => onValueChanged(value);
  }

  /// <summary>
  /// 绑定Slider事件
  /// </summary>
  public void BindSlider(Slider slider, Action<float> onValueChanged)
  {
    if (slider == null || onValueChanged == null) return;

    slider.onValueChanged.AddListener(InvokeOnValueChanged);
    _cleanupActions.Add(() => slider.onValueChanged.RemoveListener(InvokeOnValueChanged));

    void InvokeOnValueChanged(float value) => onValueChanged(value);
  }

  /// <summary>
  /// 绑定InputField事件
  /// </summary>
  public void BindInputField(InputField inputField, Action<string> onValueChanged, Action<string> onEndEdit = null)
  {
    if (inputField == null) return;

    if (onValueChanged != null)
    {
      inputField.onValueChanged.AddListener(InvokeOnValueChanged);
      _cleanupActions.Add(() => inputField.onValueChanged.RemoveListener(InvokeOnValueChanged));

      void InvokeOnValueChanged(string value) => onValueChanged(value);
    }

    if (onEndEdit != null)
    {
      inputField.onEndEdit.AddListener(InvokeOnEndEdit);
      _cleanupActions.Add(() => inputField.onEndEdit.RemoveListener(InvokeOnEndEdit));

      void InvokeOnEndEdit(string value) => onEndEdit(value);
    }
  }

  /// <summary>
  /// 清理所有绑定
  /// </summary>
  public void Clear()
  {
    foreach (var action in _cleanupActions)
    {
      action?.Invoke();
    }
    _cleanupActions.Clear();
  }
}

/// <summary>
/// UI组件缓存器
/// 用于缓存UI组件引用，避免重复查找
/// </summary>
public class UIComponentCache
{
  private readonly Transform _root;
  private readonly Dictionary<string, Component> _cache = new Dictionary<string, Component>();

  public UIComponentCache(Transform root)
  {
    _root = root;
  }

  /// <summary>
  /// 获取组件（带缓存）
  /// </summary>
  public T Get<T>(string path) where T : Component
  {
    string key = $"{path}_{typeof(T).Name}";

    if (_cache.TryGetValue(key, out var cached))
    {
      return cached as T;
    }

    var child = _root.Find(path);
    if (child == null)
    {
      Debug.LogWarning($"[{nameof(UIComponentCache)}] Path not found: {path}");
      return null;
    }

    var component = child.GetComponent<T>();
    if (component != null)
    {
      _cache[key] = component;
    }

    return component;
  }

  /// <summary>
  /// 清理缓存
  /// </summary>
  public void Clear()
  {
    _cache.Clear();
  }
}