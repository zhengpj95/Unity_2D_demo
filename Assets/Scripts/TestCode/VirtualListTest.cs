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
  [SerializeField] private Transform btnRefresh;

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

    // 设置点击回调
    list.onItemClick = OnListItemClick;
    list2.onItemClick = OnListItemClick;

    list.RefreshData(datas);
    list2.RefreshData(datas);

    btnRefresh.GetChild(0).GetComponent<TMP_Text>().text = "点击跳转";
    // btnRefresh.GetComponentInChildren<TMP_Text>().text = "点击跳转";
  }

  public void OnClickJump()
  {
    if (list2 != null)
    {
      list2.ScrollToIndex(5, true);
    }
  }

  /// <summary>
  /// item 渲染回调函数
  /// </summary>
  private void OnRenderItem(VirtualListRenderInfo info)
  {
    // 从 itemTransform 获取 RankItem 组件
    var rankItem = info.itemTransform.GetComponent<RankItem>();
    if (rankItem != null && info.data is RankData rankData)
    {
      rankItem.Refresh(info.index, rankData, info.selectedIndex == info.index);
    }
  }

  /// <summary>
  /// item 点击回调函数
  /// </summary>
  private void OnListItemClick(VirtualListRenderInfo info)
  {
    if (info.data is RankData rankData)
    {
      Debug.Log($"点击了第 {info.index} 个 item: {rankData.Name} - 分数: {rankData.Score}");
    }
  }
}