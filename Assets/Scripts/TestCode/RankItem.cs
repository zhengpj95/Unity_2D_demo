using UnityEngine;
using TMPro;

/// <summary>
/// 排行榜 item 组件
/// 监听 VirtualList 的 renderHandler 回调，处理自己的渲染逻辑
/// </summary>
public class RankItem : MonoBehaviour
{
  [SerializeField] private TextMeshProUGUI txtName;
  [SerializeField] private TextMeshProUGUI txtScore;

  /// <summary>
  /// 处理 item 的渲染逻辑
  /// </summary>
  public void Refresh(int index, RankData data)
  {
    if (data == null)
      return;

    if (txtName != null)
      txtName.text = data.Name;
    if (txtScore != null)
      txtScore.text = data.Score.ToString();
  }
}