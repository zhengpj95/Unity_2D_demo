using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用对象池管理器（纯 C# 单例模式）
/// </summary>
public class PoolManager : Singleton<PoolManager>
{
  // 存储所有对象池，Key 为 Prefab 的 InstanceID
  private readonly Dictionary<int, Stack<GameObject>> _poolDict = new Dictionary<int, Stack<GameObject>>();

  // 存储正在运行的对象与其所属 Prefab InstanceID 的映射，用于回收
  private readonly Dictionary<int, int> _instanceToPrefabId = new Dictionary<int, int>();

  // UI 专用池，Key 为 Prefab 的 InstanceID
  private readonly Dictionary<int, Stack<GameObject>> _uiPoolDict = new Dictionary<int, Stack<GameObject>>();

  // UI 对象到 Prefab ID 的映射
  private readonly Dictionary<int, int> _uiInstanceToPrefabId = new Dictionary<int, int>();

  // 对象池在场景中的根节点
  private readonly GameObject _poolRoot;

  // UI 对象池在场景中的根节点
  private GameObject _uiPoolRoot;

  // 主 Canvas（用于 UI 池）
  private Canvas _mainCanvas;

  /// <summary>
  /// 私有构造函数（由 Singleton 基类通过反射调用）
  /// </summary>
  private PoolManager()
  {
    _poolRoot = new GameObject("[PoolRoot]");
    Object.DontDestroyOnLoad(_poolRoot);
  }

  /// <summary>
  /// 初始化 UI 池（需要在游戏启动时调用）
  /// </summary>
  /// <param name="mainCanvas">主 Canvas</param>
  public void InitializeUIPool(Canvas mainCanvas)
  {
    if (mainCanvas == null)
    {
      Debug.LogWarning("Main canvas is null. UI pool will not be initialized.");
      return;
    }

    _mainCanvas = mainCanvas;
    _uiPoolRoot = new GameObject("[UIPoolRoot]");
    _uiPoolRoot.transform.SetParent(_mainCanvas.transform, false);
    _uiPoolRoot.transform.SetAsFirstSibling(); // 放在最底层
    Debug.Log($"UIPool initialized under canvas: {_mainCanvas.name}");
  }

  /// <summary>
  /// 预加载对象
  /// </summary>
  /// <param name="prefab">预设体</param>
  /// <param name="count">数量</param>
  public void Preload(GameObject prefab, int count)
  {
    if (prefab == null) return;

    int prefabId = prefab.GetInstanceID();
    if (!_poolDict.ContainsKey(prefabId))
    {
      _poolDict[prefabId] = new Stack<GameObject>();
    }

    for (int i = 0; i < count; i++)
    {
      GameObject obj = Object.Instantiate(prefab, _poolRoot.transform);
      obj.SetActive(false);
      _instanceToPrefabId[obj.GetInstanceID()] = prefabId;
      _poolDict[prefabId].Push(obj);
    }
  }

  /// <summary>
  /// 从对象池获取对象
  /// </summary>
  /// <param name="prefab">原始预设体</param>
  /// <returns>实例化的对象</returns>
  public GameObject Alloc(GameObject prefab)
  {
    if (prefab == null) return null;

    int prefabId = prefab.GetInstanceID();
    if (!_poolDict.ContainsKey(prefabId))
    {
      _poolDict[prefabId] = new Stack<GameObject>();
    }

    GameObject obj;
    if (_poolDict[prefabId].Count > 0)
    {
      obj = _poolDict[prefabId].Pop();
    }
    else
    {
      obj = Object.Instantiate(prefab);
      _instanceToPrefabId[obj.GetInstanceID()] = prefabId;
    }

    obj.SetActive(true);
    obj.transform.SetParent(null);

    // 处理 IPoolable 接口
    var poolables = obj.GetComponentsInChildren<IPoolable>();
    foreach (var p in poolables)
    {
      p.OnAlloc();
    }

    return obj;
  }

  /// <summary>
  /// 从对象池获取对象并设置位置和旋转
  /// </summary>
  public GameObject Alloc(GameObject prefab, Vector3 position, Quaternion rotation)
  {
    GameObject obj = Alloc(prefab);
    if (obj != null)
    {
      obj.transform.position = position;
      obj.transform.rotation = rotation;
    }
    return obj;
  }

  /// <summary>
  /// 从对象池获取对象并返回指定组件
  /// </summary>
  public T Alloc<T>(GameObject prefab) where T : Component
  {
    GameObject obj = Alloc(prefab);
    return obj != null ? obj.GetComponent<T>() : null;
  }

  /// <summary>
  /// 从对象池获取对象并设置位置旋转，返回指定组件
  /// </summary>
  public T Alloc<T>(GameObject prefab, Vector3 position, Quaternion rotation) where T : Component
  {
    GameObject obj = Alloc(prefab, position, rotation);
    return obj != null ? obj.GetComponent<T>() : null;
  }

  /// <summary>
  /// 将对象回收进池中
  /// </summary>
  /// <param name="obj">要回收的对象实例</param>
  public void Free(GameObject obj)
  {
    if (obj == null) return;

    int instanceId = obj.GetInstanceID();
    if (_instanceToPrefabId.TryGetValue(instanceId, out int prefabId))
    {
      // 处理 IPoolable 接口
      var poolables = obj.GetComponentsInChildren<IPoolable>();
      foreach (var p in poolables)
      {
        p.OnFree();
      }

      obj.SetActive(false);
      obj.transform.SetParent(_poolRoot.transform);
      _poolDict[prefabId].Push(obj);
    }
    else
    {
      // 如果不是从池里出的，直接销毁
      Debug.LogWarning($"Object {obj.name} was not spawned from PoolManager. Destroying it.");
      Object.Destroy(obj);
    }
  }

  /// <summary>
  /// 清空特定预设体的对象池
  /// </summary>
  public void ClearPool(GameObject prefab)
  {
    if (prefab == null) return;
    int prefabId = prefab.GetInstanceID();
    if (_poolDict.TryGetValue(prefabId, out var stack))
    {
      while (stack.Count > 0)
      {
        var obj = stack.Pop();
        if (obj != null)
        {
          _instanceToPrefabId.Remove(obj.GetInstanceID());
          Object.Destroy(obj);
        }
      }
      _poolDict.Remove(prefabId);
    }
  }

  /// <summary>
  /// 清空所有对象池
  /// </summary>
  public void ClearAll()
  {
    foreach (var stack in _poolDict.Values)
    {
      while (stack.Count > 0)
      {
        Object.Destroy(stack.Pop());
      }
    }
    _poolDict.Clear();
    _instanceToPrefabId.Clear();
  }

  public void OnUpdate()
  {
    // 可以在这里处理一些定时回收或其他逻辑
  }


  #region UI 专用方法

  /// <summary>
  /// 从对象池获取 UI 元素并设置父节点
  /// 所有 UI 对象统一从 [UIPoolRoot] 中取出，使用完放回
  /// </summary>
  /// <param name="prefab">UI 预设体</param>
  /// <param name="parent">父节点（通常是 Canvas 或其子节点）</param>
  /// <returns>实例化的 UI 元素</returns>
  public GameObject AllocUI(GameObject prefab, Transform parent)
  {
    if (prefab == null || parent == null) return null;

    int prefabId = prefab.GetInstanceID();

    // 获取或创建该 prefab 的池
    if (!_uiPoolDict.TryGetValue(prefabId, out var pool))
    {
      pool = new Stack<GameObject>();
      _uiPoolDict[prefabId] = pool;
    }

    GameObject obj;
    if (pool.Count > 0)
    {
      obj = pool.Pop();
    }
    else
    {
      obj = Object.Instantiate(prefab);
      _uiInstanceToPrefabId[obj.GetInstanceID()] = prefabId;
    }

    obj.SetActive(true);
    obj.transform.SetParent(parent, false);

    // 处理 IPoolable 接口
    var poolables = obj.GetComponentsInChildren<IPoolable>();
    foreach (var p in poolables)
    {
      p.OnAlloc();
    }

    Debug.Log($"AllocUI: prefabId={prefabId}, objName={obj.name}");
    return obj;
  }

  /// <summary>
  /// 从对象池获取 UI 元素并返回指定组件
  /// </summary>
  public T AllocUI<T>(GameObject prefab, Transform parent) where T : Component
  {
    GameObject obj = AllocUI(prefab, parent);
    return obj != null ? obj.GetComponent<T>() : null;
  }

  /// <summary>
  /// 回收 UI 元素到 [UIPoolRoot]
  /// 使用统一的 UI 池，不区分父节点
  /// </summary>
  /// <param name="obj">要回收的 UI 元素</param>
  public void FreeUI(GameObject obj)
  {
    if (obj == null) return;

    int instanceId = obj.GetInstanceID();
    if (_uiInstanceToPrefabId.TryGetValue(instanceId, out int prefabId))
    {
      // 处理 IPoolable 接口
      var poolables = obj.GetComponentsInChildren<IPoolable>();
      foreach (var p in poolables)
      {
        p.OnFree();
      }

      obj.SetActive(false);

      // 如果 UI 池已初始化，移动到 Canvas 下的 UIPoolRoot
      if (_uiPoolRoot != null)
      {
        obj.transform.SetParent(_uiPoolRoot.transform, false);
      }
      else
      {
        Debug.LogWarning("UI pool not initialized. Call InitializeUIPool(mainCanvas) first. Object will stay in current parent.");
      }

      // 放回统一的 UI 池
      if (_uiPoolDict.TryGetValue(prefabId, out var pool))
      {
        pool.Push(obj);
        Debug.Log($"FreeUI: instanceId={instanceId}, prefabId={prefabId}, objName={obj.name}");
      }
      else
      {
        Debug.LogWarning($"Pool for prefabId={prefabId} not found. Object will not be pooled.");
      }
    }
    else
    {
      Debug.LogWarning($"Object {obj.name} was not spawned from AllocUI. Destroying it.");
      Object.Destroy(obj);
    }
  }

  /// <summary>
  /// 清空特定预设体的 UI 对象池
  /// </summary>
  /// <param name="prefab">预设体</param>
  public void ClearUIPool(GameObject prefab)
  {
    if (prefab == null) return;
    int prefabId = prefab.GetInstanceID();

    if (_uiPoolDict.TryGetValue(prefabId, out var pool))
    {
      while (pool.Count > 0)
      {
        var obj = pool.Pop();
        if (obj != null)
        {
          _uiInstanceToPrefabId.Remove(obj.GetInstanceID());
          Object.Destroy(obj);
        }
      }
      _uiPoolDict.Remove(prefabId);
      Debug.Log($"ClearUIPool: prefabId={prefabId}, prefabName={prefab.name}");
    }
  }

  /// <summary>
  /// 清空所有 UI 对象池
  /// </summary>
  public void ClearAllUIPools()
  {
    foreach (var pool in _uiPoolDict.Values)
    {
      while (pool.Count > 0)
      {
        Object.Destroy(pool.Pop());
      }
    }
    _uiPoolDict.Clear();
    _uiInstanceToPrefabId.Clear();
    Debug.Log("ClearAllUIPools: All UI pools cleared");
  }

  #endregion
}
