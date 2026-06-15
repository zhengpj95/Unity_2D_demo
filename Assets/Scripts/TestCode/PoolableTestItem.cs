using UnityEngine;

/// <summary>
/// 一个实现了 IPoolable 的测试组件
/// </summary>
public class PoolableTestItem : MonoBehaviour, IPoolable
{
  public void OnAlloc()
  {
    Debug.Log($"{gameObject.name} 出池了！可以在这里重置状态。");
  }

  public void OnFree()
  {
    Debug.Log($"{gameObject.name} 入池了！");
  }
}
