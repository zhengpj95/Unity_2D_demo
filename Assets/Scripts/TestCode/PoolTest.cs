using UnityEngine;

/// <summary>
/// 对象池测试示例
/// </summary>
public class PoolTest : MonoBehaviour
{
  public GameObject testPrefab;
  private GameObject _currentInstance;

  void Update()
  {
    // 按 S 生成对象
    if (Input.GetKeyDown(KeyCode.T))
    {
      _currentInstance = PoolManager.Instance.Alloc(testPrefab, Random.insideUnitSphere * 5, Quaternion.identity);
      Debug.Log($"Allocated: {_currentInstance.name}");
    }

    // 按 D 回收对象
    if (Input.GetKeyDown(KeyCode.Y))
    {
      if (_currentInstance != null)
      {
        PoolManager.Instance.Free(_currentInstance);
        Debug.Log($"Freed: {_currentInstance.name}");
        _currentInstance = null;
      }
    }
  }
}

