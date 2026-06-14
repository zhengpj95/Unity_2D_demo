using UnityEngine;
using TMPro;

public class RankItem : VirtualListItem
{
  [SerializeField] private TextMeshProUGUI txtName;

  [SerializeField] private TextMeshProUGUI txtScore;

  protected override void OnRefresh(object data)
  {
    RankData rankData = (RankData)data;

    txtName.text = rankData.Name;
    txtScore.text = rankData.Score.ToString();
  }
}