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

    list.RefreshData(datas);
  }
}