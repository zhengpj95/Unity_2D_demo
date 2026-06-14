using UnityEngine;

public abstract class VirtualListItem : MonoBehaviour
{
  /// 当前索引
  public int Index { get; private set; }

  /// 数据刷新
  public void Refresh(int index, object data)
  {
    Index = index;

    OnRefresh(data);
  }

  protected abstract void OnRefresh(object data);
}