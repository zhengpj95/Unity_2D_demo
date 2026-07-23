using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用对象池管理器
/// </summary>
public class PoolManager : SingletonMono<PoolManager>
{
    // 存储所有对象池，Key 为 Prefab 的 InstanceID
    private readonly Dictionary<int, Stack<GameObject>> _poolDict = new Dictionary<int, Stack<GameObject>>();
    
    // 存储正在运行的对象与其所属 Prefab InstanceID 的映射，用于回收
    private readonly Dictionary<int, int> _instanceToPrefabId = new Dictionary<int, int>();

    // 对象池在场景中的根节点
    private Transform _poolRoot;

    protected override void Awake()
    {
        base.Awake();
        _poolRoot = new GameObject("PoolRoot").transform;
        _poolRoot.SetParent(transform);
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
            GameObject obj = Instantiate(prefab, _poolRoot);
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
            obj = Instantiate(prefab);
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
            obj.transform.SetParent(_poolRoot);
            _poolDict[prefabId].Push(obj);
        }
        else
        {
            // 如果不是从池里出的，直接销毁
            Debug.LogWarning($"Object {obj.name} was not spawned from PoolManager. Destroying it.");
            Destroy(obj);
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
                    Destroy(obj);
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
                Destroy(stack.Pop());
            }
        }
        _poolDict.Clear();
        _instanceToPrefabId.Clear();
    }
}
