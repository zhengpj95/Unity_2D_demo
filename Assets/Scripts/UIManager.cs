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

public class UIManager : MonoBehaviour
{
  public static UIManager Instance { get; private set; }

  public Transform mainLayer;
  public Transform windowLayer;
  public Transform modelLayer;
  public Transform tipLayer;
  private Dictionary<string, GameObject> _uiCache;

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
      DontDestroyOnLoad(gameObject);
    }

    _uiCache = new Dictionary<string, GameObject>();
  }

  // 打开界面
  public void ShowUI(string prefabPath, UILayerIndex layer)
  {
    if (!_uiCache.TryGetValue(prefabPath, out GameObject uiObj) || uiObj == null)
    {
      var prefab = Resources.Load<GameObject>(prefabPath);
      Debug.Log("UIManager.Instance.ShowUI: " + prefabPath);
      if (prefab == null)
      {
        Debug.LogError("prefab is null");
        return;
      }

      var parent = layer switch
      {
        UILayerIndex.Main => mainLayer,
        UILayerIndex.Window => windowLayer,
        UILayerIndex.Model => modelLayer,
        UILayerIndex.Tip => tipLayer,
        _ => mainLayer
      };
      uiObj = Instantiate(prefab, parent);
      _uiCache[prefabPath] = uiObj;
    }

    uiObj.SetActive(true);
    uiObj.transform.SetAsLastSibling(); // 保证在父层级的最上方显示（显示在所有兄弟 UI 的最上方）
  }

  // 关闭界面
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
}