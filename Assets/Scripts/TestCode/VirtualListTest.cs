using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RankData
{
  public string Name;

  public int Score;
}

public class VirtualListTest : MonoBehaviour
{
  [SerializeField] private VirtualList list;
  [SerializeField] private VirtualList list2;

  private void Start()
  {
    List<RankData> datas = new();

    for (int i = 0; i < 100; i++)
    {
      datas.Add(new RankData
      {
        Name = $"玩家{i}",
        Score = i * 100
      });
    }

    // 设置 renderHandler 回调
    list.renderHandler = OnRenderItem;
    list2.renderHandler = OnRenderItem;

    list.RefreshData(datas);
    list2.RefreshData(datas);
  }

  /// <summary>
  /// item 渲染回调函数
  /// </summary>
  private void OnRenderItem(int index, object data, RectTransform itemTransform)
  {
    // 从 itemTransform 获取 RankItem 组件
    var rankItem = itemTransform.GetComponent<RankItem>();
    if (rankItem != null && data is RankData rankData)
    {
      rankItem.Refresh(index, rankData);
    }
  }
}