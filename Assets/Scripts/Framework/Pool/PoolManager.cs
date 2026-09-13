using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用对象池管理器（纯 C# 单例模式）
/// </summary>
public class PoolManager : Singleton<PoolManager>
{
  private const int DefaultMaxCachedCount = 64;
  private const float DefaultIdleLifetime = 30f;
  private const float TrimInterval = 5f;
  private const int TrimBudgetPerInterval = 16;

  /// <summary>记录一个已回收实例及其进入空闲状态的时间。</summary>
  private sealed class PooledItem
  {
    public GameObject Instance;
    public int InstanceId;
    public float ReleasedAt;
  }

  /// <summary>保存单个 Prefab 的缓存对象和缩容策略。</summary>
  private sealed class PoolEntry
  {
    // 尾部用于 LIFO 复用，头部保存最早归还的对象，便于定时缩容。
    public readonly LinkedList<PooledItem> CachedItems = new LinkedList<PooledItem>();
    public int MinRetained;
    public int MaxCachedCount = DefaultMaxCachedCount;
    public float IdleLifetime = DefaultIdleLifetime;
  }

  // 存储所有普通对象池，Key 为 Prefab 的 InstanceID。
  private readonly Dictionary<int, PoolEntry> _poolDict = new Dictionary<int, PoolEntry>();

  // 存储正在运行的对象与其所属 Prefab InstanceID 的映射，用于回收
  private readonly Dictionary<int, int> _instanceToPrefabId = new Dictionary<int, int>();

  // 防止同一实例被重复压入缓存集合，避免一次实例被连续分配给多个调用方。
  private readonly HashSet<int> _inactiveInstanceIds = new HashSet<int>();

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

  // 使用非缩放时间按固定间隔缩容，暂停和 GameOver 状态下也能释放长期空闲缓存。
  private float _trimElapsed;

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
  /// 确保指定 Prefab 的池中至少缓存目标数量的可用对象；已有缓存会被计入，不重复预加载。
  /// </summary>
  /// <param name="prefab">预设体</param>
  /// <param name="count">期望的最少可用缓存数量</param>
  public void Preload(GameObject prefab, int count)
  {
    if (prefab == null || count <= 0) return;

    int prefabId = prefab.GetInstanceID();
    PoolEntry pool = GetOrCreatePool(prefabId);
    RemoveDestroyedCachedItems(pool);

    // 预加载数量同时作为该池的最低保留量；若超过默认上限，则同步扩大上限以兑现预加载请求。
    pool.MinRetained = Mathf.Max(pool.MinRetained, count);
    pool.MaxCachedCount = Mathf.Max(pool.MaxCachedCount, pool.MinRetained);

    // Preload 表示目标缓存量。重复进入场景时只补齐缺口，避免每次都追加 count 个实例。
    int missingCount = Mathf.Max(0, count - pool.CachedItems.Count);
    for (int i = 0; i < missingCount; i++)
    {
      GameObject obj = Object.Instantiate(prefab, _poolRoot.transform);
      obj.SetActive(false);
      int instanceId = obj.GetInstanceID();
      _instanceToPrefabId[instanceId] = prefabId;
      AddCachedItem(pool, obj, instanceId);
    }
  }

  /// <summary>
  /// 配置指定 Prefab 的空闲缓存策略。该策略只限制隐藏缓存，不限制场上活跃对象的创建数量。
  /// </summary>
  /// <param name="prefab">需要配置的 Prefab。</param>
  /// <param name="minRetained">定时缩容后至少保留的缓存数量。</param>
  /// <param name="maxCachedCount">允许保留在 PoolRoot 中的最大缓存数量。</param>
  /// <param name="idleLifetime">对象空闲多少秒后允许被定时销毁，使用非缩放时间。</param>
  public void ConfigurePool(GameObject prefab, int minRetained, int maxCachedCount, float idleLifetime)
  {
    if (prefab == null)
      return;

    minRetained = Mathf.Max(0, minRetained);
    maxCachedCount = Mathf.Max(minRetained, maxCachedCount);
    idleLifetime = Mathf.Max(0f, idleLifetime);

    int prefabId = prefab.GetInstanceID();
    PoolEntry pool = GetOrCreatePool(prefabId);
    RemoveDestroyedCachedItems(pool);
    pool.MinRetained = minRetained;
    pool.MaxCachedCount = maxCachedCount;
    pool.IdleLifetime = idleLifetime;
    TrimToMaximum(pool);
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
    PoolEntry pool = GetOrCreatePool(prefabId);

    GameObject obj = null;
    while (pool.CachedItems.Count > 0 && obj == null)
    {
      LinkedListNode<PooledItem> node = pool.CachedItems.Last;
      PooledItem item = node.Value;
      pool.CachedItems.RemoveLast();
      _inactiveInstanceIds.Remove(item.InstanceId);

      obj = item.Instance;
      if (obj == null)
        _instanceToPrefabId.Remove(item.InstanceId);
    }

    if (obj == null)
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
      if (_inactiveInstanceIds.Contains(instanceId))
      {
        Debug.LogWarning($"Object {obj.name} has already been returned to PoolManager.", obj);
        return;
      }

      // 处理 IPoolable 接口
      var poolables = obj.GetComponentsInChildren<IPoolable>();
      foreach (var p in poolables)
      {
        p.OnFree();
      }

      // OnFree 或场景卸载可能在回调期间销毁对象；退出 Play Mode 时持久化 PoolRoot 也会一起销毁。
      // 此时不能再访问 Transform 或压入池栈，否则会触发 MissingReferenceException。
      if (obj == null || _poolRoot == null)
      {
        _instanceToPrefabId.Remove(instanceId);
        return;
      }

      obj.SetActive(false);

      // SetActive 的生命周期回调也可能销毁对象，因此设父节点前再次确认有效性。
      if (obj == null || _poolRoot == null)
      {
        _instanceToPrefabId.Remove(instanceId);
        return;
      }

      // ClearPool/ClearAll 后仍在场上的对象可以完成 OnFree，但不再重建已释放的池。
      if (!_poolDict.TryGetValue(prefabId, out PoolEntry pool))
      {
        _instanceToPrefabId.Remove(instanceId);
        Object.Destroy(obj);
        return;
      }

      // 缓存达到上限时销毁多余实例；MaxCachedCount 不会影响仍在场上的活跃对象。
      if (pool.CachedItems.Count >= pool.MaxCachedCount)
      {
        _instanceToPrefabId.Remove(instanceId);
        Object.Destroy(obj);
        return;
      }

      obj.transform.SetParent(_poolRoot.transform);
      AddCachedItem(pool, obj, instanceId);
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
    if (_poolDict.TryGetValue(prefabId, out PoolEntry pool))
    {
      while (pool.CachedItems.Count > 0)
      {
        DestroyCachedItem(pool.CachedItems.First.Value);
        pool.CachedItems.RemoveFirst();
      }
      _poolDict.Remove(prefabId);
    }
  }

  /// <summary>
  /// 清空所有对象池
  /// </summary>
  public void ClearAll()
  {
    foreach (PoolEntry pool in _poolDict.Values)
    {
      while (pool.CachedItems.Count > 0)
      {
        DestroyCachedItem(pool.CachedItems.First.Value);
        pool.CachedItems.RemoveFirst();
      }
    }
    _poolDict.Clear();
    _inactiveInstanceIds.Clear();
  }

  /// <summary>
  /// 按固定间隔分批销毁超过空闲期限的缓存对象，避免每帧扫描和同一帧集中 Destroy。
  /// </summary>
  public void OnUpdate()
  {
    _trimElapsed += Time.unscaledDeltaTime;
    if (_trimElapsed < TrimInterval)
      return;

    _trimElapsed = 0f;
    float now = Time.realtimeSinceStartup;
    int remainingBudget = TrimBudgetPerInterval;

    foreach (PoolEntry pool in _poolDict.Values)
    {
      while (pool.CachedItems.Count > pool.MinRetained && remainingBudget > 0)
      {
        PooledItem item = pool.CachedItems.First.Value;
        if (item.Instance != null && now - item.ReleasedAt < pool.IdleLifetime)
          break;

        pool.CachedItems.RemoveFirst();
        DestroyCachedItem(item);
        remainingBudget--;
      }

      if (remainingBudget <= 0)
        break;
    }
  }

  /// <summary>获取或创建指定 Prefab 的普通对象池配置。</summary>
  private PoolEntry GetOrCreatePool(int prefabId)
  {
    if (!_poolDict.TryGetValue(prefabId, out PoolEntry pool))
    {
      pool = new PoolEntry();
      _poolDict[prefabId] = pool;
    }
    return pool;
  }

  /// <summary>记录一个已禁用并归还到 PoolRoot 的实例。</summary>
  private void AddCachedItem(PoolEntry pool, GameObject instance, int instanceId)
  {
    pool.CachedItems.AddLast(new PooledItem
    {
      Instance = instance,
      InstanceId = instanceId,
      ReleasedAt = Time.realtimeSinceStartup
    });
    _inactiveInstanceIds.Add(instanceId);
  }

  /// <summary>立即将指定池缩减到最大缓存数量。</summary>
  private void TrimToMaximum(PoolEntry pool)
  {
    while (pool.CachedItems.Count > pool.MaxCachedCount)
    {
      PooledItem item = pool.CachedItems.First.Value;
      pool.CachedItems.RemoveFirst();
      DestroyCachedItem(item);
    }
  }

  /// <summary>清除被外部销毁的缓存引用，避免失效节点占用预加载数量。</summary>
  private void RemoveDestroyedCachedItems(PoolEntry pool)
  {
    LinkedListNode<PooledItem> node = pool.CachedItems.First;
    while (node != null)
    {
      LinkedListNode<PooledItem> next = node.Next;
      if (node.Value.Instance == null)
      {
        pool.CachedItems.Remove(node);
        DestroyCachedItem(node.Value);
      }
      node = next;
    }
  }

  /// <summary>销毁一个缓存实例，并同步清理普通池的索引。</summary>
  private void DestroyCachedItem(PooledItem item)
  {
    _inactiveInstanceIds.Remove(item.InstanceId);
    _instanceToPrefabId.Remove(item.InstanceId);
    if (item.Instance != null)
      Object.Destroy(item.Instance);
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
