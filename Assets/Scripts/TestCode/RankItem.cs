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
  [SerializeField] private CanvasGroup canvasGroup;
  [SerializeField] private Color selectedColor = Color.yellow;
  [SerializeField] private Color normalColor = Color.white;

  private Color _originalNameColor;
  private int _fontSize = 36;

  private void OnEnable()
  {
    if (txtName != null)
    {
      _originalNameColor = txtName.color;
    }
  }

  /// <summary>
  /// 处理 item 的渲染逻辑
  /// </summary>
  public void Refresh(int index, RankData data, bool isSelected)
  {
    if (data == null)
      return;

    // 更新数据
    if (txtName != null)
      txtName.text = data.Name;
    if (txtScore != null)
      txtScore.text = data.Score.ToString();

    // 根据选中状态应用视觉效果
    ApplySelectedState(isSelected);
  }

  /// <summary>
  /// 应用选中状态的视觉效果
  /// </summary>
  private void ApplySelectedState(bool isSelected)
  {
    if (isSelected)
    {
      // 选中状态：高亮显示
      if (txtName != null)
      {
        txtName.color = selectedColor;
        txtName.fontSize = _fontSize * 1.1f;
      }

      if (txtScore != null)
      {
        txtScore.color = selectedColor;
        txtScore.fontSize = _fontSize * 1.1f;
      }

      // 设置Canvas Group的透明度或其他效果
      if (canvasGroup != null)
      {
        canvasGroup.alpha = 1f;
      }
    }
    else
    {
      // 非选中状态：恢复正常
      if (txtName != null)
      {
        txtName.color = normalColor;
        txtName.fontSize = _fontSize / 1.1f;
      }

      if (txtScore != null)
      {
        txtScore.color = normalColor;
        txtScore.fontSize = _fontSize / 1.1f;
      }

      // 设置Canvas Group的透明度
      if (canvasGroup != null)
      {
        canvasGroup.alpha = 0.7f;
      }
    }
  }
}