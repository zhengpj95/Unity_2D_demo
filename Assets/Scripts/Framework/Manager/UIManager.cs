using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UILayerIndex
{
  Main = 0,
  Window = 1,
  Model = 2,
  Tip = 3,
}

/// <summary>
/// UI管理器
/// 负责UI的显示、隐藏、层级管理和缓存
/// </summary>
public class UIManager : MonoBehaviour
{
  #region 单例模式

  public static UIManager Instance { get; private set; }

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
      DontDestroyOnLoad(gameObject);
    }

    _uiCache = new Dictionary<string, GameObject>();
  }

  private void OnDestroy()
  {
    if (Instance == this)
    {
      Instance = null;
    }
  }

  #endregion

  #region UI层级

  [Header("UI Layers")]
  public Transform mainLayer;
  public Transform windowLayer;
  public Transform modelLayer;
  public Transform tipLayer;

  #endregion

  #region UI缓存

  private Dictionary<string, GameObject> _uiCache;

  #endregion

  #region 基础功能

  /// <summary>
  /// 显示UI
  /// </summary>
  /// <param name="prefabPath">Prefab路径（Resources下的相对路径）</param>
  /// <param name="layer">UI层级</param>
  public void ShowUI(string prefabPath, UILayerIndex layer)
  {
    if (!_uiCache.TryGetValue(prefabPath, out GameObject uiObj) || uiObj == null)
    {
      var prefab = Resources.Load<GameObject>(prefabPath);
      Debug.Log($"[UIManager] ShowUI: {prefabPath}");
      if (prefab == null)
      {
        Debug.LogError($"[UIManager] Prefab not found: {prefabPath}");
        return;
      }

      var parent = GetLayerParent(layer);
      uiObj = Instantiate(prefab, parent);
      _uiCache[prefabPath] = uiObj;
    }

    uiObj.SetActive(true);
    uiObj.transform.SetAsLastSibling();
  }

  /// <summary>
  /// 隐藏UI
  /// </summary>
  /// <param name="prefabPath">Prefab路径</param>
  /// <param name="isDestroy">是否销毁GameObject</param>
  public void HideUI(string prefabPath, bool isDestroy = false)
  {
    if (_uiCache.TryGetValue(prefabPath, out GameObject ui))
    {
      if (isDestroy)
      {
        Destroy(ui);
        _uiCache.Remove(prefabPath);
      }
      else
      {
        ui.SetActive(false);
      }
    }
  }

  /// <summary>
  /// 获取UI GameObject实例
  /// </summary>
  /// <param name="prefabPath">Prefab路径</param>
  /// <returns>GameObject实例，不存在返回null</returns>
  public GameObject GetUIObject(string prefabPath)
  {
    if (_uiCache.TryGetValue(prefabPath, out GameObject uiObj))
    {
      return uiObj;
    }
    return null;
  }

  /// <summary>
  /// 获取UI上的组件
  /// </summary>
  /// <typeparam name="T">组件类型</typeparam>
  /// <param name="prefabPath">Prefab路径</param>
  /// <returns>组件实例</returns>
  public T GetUIComponent<T>(string prefabPath) where T : Component
  {
    var go = GetUIObject(prefabPath);
    if (go == null) return null;
    return go.GetComponent<T>();
  }

  /// <summary>
  /// 检查UI是否存在
  /// </summary>
  /// <param name="prefabPath">Prefab路径</param>
  /// <returns>是否存在</returns>
  public bool HasUI(string prefabPath)
  {
    return _uiCache.ContainsKey(prefabPath);
  }

  /// <summary>
  /// 销毁指定UI
  /// </summary>
  /// <param name="prefabPath">Prefab路径</param>
  public void DestroyUI(string prefabPath)
  {
    HideUI(prefabPath, true);
  }

  /// <summary>
  /// 销毁所有UI
  /// </summary>
  public void DestroyAllUI()
  {
    foreach (var kvp in _uiCache)
    {
      if (kvp.Value != null)
      {
        Destroy(kvp.Value);
      }
    }
    _uiCache.Clear();
  }

  /// <summary>
  /// 获取层级父节点
  /// </summary>
  private Transform GetLayerParent(UILayerIndex layer)
  {
    return layer switch
    {
      UILayerIndex.Main => mainLayer,
      UILayerIndex.Window => windowLayer,
      UILayerIndex.Model => modelLayer,
      UILayerIndex.Tip => tipLayer,
      _ => mainLayer
    };
  }

  #endregion
}